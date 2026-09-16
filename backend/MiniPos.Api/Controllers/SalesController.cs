using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SalesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public ActionResult<PagedResponse<SaleListItemResponse>> GetSales(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? customerId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;

        var query = _db.Sales.AsQueryable();

        if (from is not null)
        {
            var start = DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt >= start);
        }

        if (to is not null)
        {
            var end = DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt < end);
        }

        if (customerId is not null)
            query = query.Where(s => s.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);

        var total = query.Count();

        var items = query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SaleListItemResponse
            {
                Id = s.Id,
                Number = s.Number,
                CreatedAt = s.CreatedAt,
                CustomerName = s.Customer != null ? s.Customer.FullName : null,
                ItemCount = s.Items.Count,
                Total = s.Total,
                PaymentMethod = s.PaymentMethod,
                Status = s.Status
            })
            .ToList();

        return new PagedResponse<SaleListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    [HttpGet("{id}")]
    public ActionResult<SaleResponse> GetSale(int id)
    {
        var sale = _db.Sales
            .Include(s => s.Customer)
            .Include(s => s.User)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefault(s => s.Id == id);

        if (sale is null)
            return NotFound(new { error = "Sale not found." });

        return ToResponse(sale);
    }

    // Read the stock rows for update and decrement them in the same transaction as the insert,
    // otherwise two cashiers can sell the last unit at the same time.
    [HttpPost]
    public ActionResult<SaleResponse> CreateSale(CreateSaleRequest request)
    {
        if (request.Items.Count == 0)
            return BadRequest(new { error = "Add at least one item to the sale." });

        if (request.Items.Any(i => i.Quantity <= 0))
            return BadRequest(new { error = "Every item needs a quantity of at least 1." });

        var method = request.PaymentMethod?.Trim().ToLower() ?? string.Empty;
        if (method != "cash" && method != "card" && method != "izipay")
            return BadRequest(new { error = "Payment method must be cash, card or izipay." });

        if (request.Discount < 0)
            return BadRequest(new { error = "Discount cannot be negative." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (request.CustomerId is not null && !_db.Customers.Any(c => c.Id == request.CustomerId.Value))
            return BadRequest(new { error = "The selected customer does not exist." });

        using var transaction = _db.Database.BeginTransaction();

        var ids = request.Items.Select(i => i.ProductId).Distinct().OrderBy(i => i).ToList();

        var products = _db.Products
            .FromSql($@"SELECT * FROM ""Products"" WHERE ""Id"" = ANY({ids}) ORDER BY ""Id"" FOR UPDATE")
            .ToList();

        foreach (var line in request.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == line.ProductId);
            if (product is null || !product.IsActive)
                return BadRequest(new { error = $"Product {line.ProductId} is missing or inactive." });
        }

        foreach (var group in request.Items.GroupBy(i => i.ProductId))
        {
            var product = products.First(p => p.Id == group.Key);
            var wanted = group.Sum(i => i.Quantity);
            if (product.Stock < wanted)
                return BadRequest(new { error = $"Not enough stock for {product.Name}. Only {product.Stock} left." });
        }

        var subtotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
        if (request.Discount > subtotal)
            return BadRequest(new { error = "Discount cannot be larger than the subtotal." });

        var total = subtotal - request.Discount;
        if (method == "cash" && request.PaidAmount < total)
            return BadRequest(new { error = "Paid amount is less than the total." });

        var sale = new Sale
        {
            Number = NextSaleNumber(),
            CreatedAt = DateTime.UtcNow,
            CustomerId = request.CustomerId,
            UserId = userId,
            Subtotal = subtotal,
            Discount = request.Discount,
            Total = total,
            PaidAmount = request.PaidAmount,
            ChangeAmount = method == "cash" ? request.PaidAmount - total : 0m,
            RefundedAmount = 0m,
            PaymentMethod = method,
            Status = "completed"
        };

        foreach (var line in request.Items)
        {
            sale.Items.Add(new SaleItem
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = line.Quantity * line.UnitPrice,
                ReturnedQuantity = 0
            });

            products.First(p => p.Id == line.ProductId).Stock -= line.Quantity;
        }

        _db.Sales.Add(sale);
        _db.SaveChanges();
        transaction.Commit();

        var created = _db.Sales
            .Include(s => s.Customer)
            .Include(s => s.User)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .First(s => s.Id == sale.Id);

        return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, ToResponse(created));
    }

    [HttpPost("{id}/return")]
    [Authorize(Roles = "Admin,Manager")]
    public ActionResult<SaleResponse> ReturnSale(int id, CreateReturnRequest request)
    {
        if (request.Items.Count == 0)
            return BadRequest(new { error = "Select at least one item to return." });

        if (request.Items.Any(i => i.Quantity <= 0))
            return BadRequest(new { error = "Every returned item needs a quantity of at least 1." });

        using var transaction = _db.Database.BeginTransaction();

        var sale = _db.Sales
            .Include(s => s.Items)
            .FirstOrDefault(s => s.Id == id);

        if (sale is null)
            return NotFound(new { error = "Sale not found." });

        if (sale.Status == "returned")
            return BadRequest(new { error = "This sale is already fully returned." });

        foreach (var group in request.Items.GroupBy(i => i.SaleItemId))
        {
            var line = sale.Items.FirstOrDefault(i => i.Id == group.Key);
            if (line is null)
                return BadRequest(new { error = "One of the selected lines is not part of this sale." });

            var wanted = group.Sum(i => i.Quantity);
            if (wanted > line.Quantity - line.ReturnedQuantity)
                return BadRequest(new { error = $"Cannot return {wanted} of this line. Only {line.Quantity - line.ReturnedQuantity} left to return." });
        }

        var productIds = request.Items
            .Select(i => sale.Items.First(l => l.Id == i.SaleItemId).ProductId)
            .Distinct()
            .OrderBy(i => i)
            .ToList();

        var products = _db.Products
            .FromSql($@"SELECT * FROM ""Products"" WHERE ""Id"" = ANY({productIds}) ORDER BY ""Id"" FOR UPDATE")
            .ToList();

        var refunded = 0m;

        foreach (var item in request.Items)
        {
            var line = sale.Items.First(l => l.Id == item.SaleItemId);

            line.ReturnedQuantity += item.Quantity;
            refunded += item.Quantity * line.UnitPrice;
            products.First(p => p.Id == line.ProductId).Stock += item.Quantity;
        }

        sale.RefundedAmount += refunded;
        sale.Status = sale.Items.All(i => i.ReturnedQuantity >= i.Quantity) ? "returned" : "partiallyReturned";

        _db.SaveChanges();
        transaction.Commit();

        var updated = _db.Sales
            .Include(s => s.Customer)
            .Include(s => s.User)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .First(s => s.Id == id);

        return ToResponse(updated);
    }

    private string NextSaleNumber()
    {
        var prefix = $"S-{DateTime.UtcNow:yyyyMMdd}-";

        var last = _db.Sales
            .Where(s => s.Number.StartsWith(prefix))
            .OrderByDescending(s => s.Number)
            .Select(s => s.Number)
            .FirstOrDefault();

        var next = last is null ? 1 : int.Parse(last[prefix.Length..]) + 1;

        return prefix + next.ToString("D4");
    }

    private static SaleResponse ToResponse(Sale sale) => new()
    {
        Id = sale.Id,
        Number = sale.Number,
        CreatedAt = sale.CreatedAt,
        CustomerId = sale.CustomerId,
        CustomerName = sale.Customer?.FullName,
        CashierName = sale.User?.FullName ?? string.Empty,
        Items = sale.Items.Select(i => new SaleItemResponse
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Sku = i.Product?.Sku ?? string.Empty,
            ProductName = i.Product?.Name ?? string.Empty,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal,
            ReturnedQuantity = i.ReturnedQuantity
        }).ToList(),
        Subtotal = sale.Subtotal,
        Discount = sale.Discount,
        Total = sale.Total,
        PaidAmount = sale.PaidAmount,
        ChangeAmount = sale.ChangeAmount,
        RefundedAmount = sale.RefundedAmount,
        PaymentMethod = sale.PaymentMethod,
        Status = sale.Status
    };
}
