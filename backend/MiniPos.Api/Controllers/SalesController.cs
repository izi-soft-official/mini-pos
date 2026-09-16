using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sales")]
public class SalesController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<SaleListItemResponse>>> GetSales(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? customerId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var fromDt = from.HasValue ? from.Value.ToDateTime(new TimeOnly(0)) : DateTime.MinValue;
        var toDt = to.HasValue ? to.Value.ToDateTime(new TimeOnly(23, 59, 59)) : DateTime.MaxValue;

        var query = _db.Sales.Include(s => s.Items).AsQueryable();
        query = query.Where(s => s.CreatedAt >= fromDt && s.CreatedAt <= toDt);
        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new SaleListItemResponse(s.Id, s.Number, s.CreatedAt, s.Customer != null ? s.Customer.FullName : null, s.Items.Count, s.Total, s.PaymentMethod, s.Status))
            .ToListAsync();

        var resp = new PagedResponse<SaleListItemResponse> { Items = items, Page = page, PageSize = pageSize, Total = total };
        return Ok(resp);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaleResponse>> GetSale(int id)
    {
        var sale = await _db.Sales.Include(s => s.Items).ThenInclude(i => i.Product).Include(s => s.Customer).Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null) return NotFound(new { error = "Sale not found." });

        var items = sale.Items.Select(i => new SaleItemResponse(i.Id, i.ProductId, i.Product?.Sku ?? string.Empty, i.Product?.Name ?? string.Empty, i.Quantity, i.UnitPrice, i.LineTotal, i.ReturnedQuantity)).ToList();

        var resp = new SaleResponse(sale.Id, sale.Number, sale.CreatedAt, sale.CustomerId, sale.Customer?.FullName, sale.User?.FullName ?? string.Empty, items, sale.Subtotal, sale.Discount, sale.Total, sale.PaidAmount, sale.ChangeAmount, sale.RefundedAmount, sale.PaymentMethod, sale.Status);
        return Ok(resp);
    }

    [HttpPost]
    public async Task<ActionResult<SaleResponse>> CreateSale(CreateSaleRequest request)
    {
        if (request.Items == null || request.Items.Count == 0) return BadRequest(new { error = "No items provided" });

        int? userId = null;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(claim) && int.TryParse(claim, out var uid)) userId = uid;

        // Basic create logic: validate products, adjust stock, compute totals
        var sale = new Sale { Number = $"S{DateTime.UtcNow:yyyyMMddHHmmss}", CreatedAt = DateTime.UtcNow, CustomerId = request.CustomerId, PaymentMethod = request.PaymentMethod, Discount = request.Discount, PaidAmount = request.PaidAmount, Status = "Completed", UserId = userId };
        sale.Items = new List<SaleItem>();

        decimal subtotal = 0m;
        foreach (var it in request.Items)
        {
            var product = await _db.Products.FindAsync(it.ProductId);
            if (product == null) return BadRequest(new { error = $"Product {it.ProductId} not found" });
            if (product.Stock < it.Quantity) return BadRequest(new { error = $"Insufficient stock for product {product.Name}" });

            product.Stock -= it.Quantity;
            var lineTotal = it.UnitPrice * it.Quantity;
            subtotal += lineTotal;

            var saleItem = new SaleItem { ProductId = product.Id, Quantity = it.Quantity, UnitPrice = it.UnitPrice, LineTotal = lineTotal };
            sale.Items.Add(saleItem);
        }

        sale.Subtotal = subtotal;
        sale.Total = subtotal - sale.Discount;
        sale.ChangeAmount = sale.PaidAmount - sale.Total;

        await _db.Sales.AddAsync(sale);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, sale);
    }

    [HttpPost("{id:int}/return")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SaleResponse>> ReturnSale(int id, CreateReturnRequest request)
    {
        var sale = await _db.Sales.Include(s => s.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null) return NotFound(new { error = "Sale not found." });

        // Simple return handling: increase stock, adjust refunded amount
        decimal refund = 0m;
        foreach (var item in request.Items)
        {
            var saleItem = sale.Items.FirstOrDefault(si => si.Id == item.SaleItemId);
            if (saleItem == null) return BadRequest(new { error = $"Sale item {item.SaleItemId} not found" });
            if (item.Quantity <= 0 || item.Quantity > (saleItem.Quantity - saleItem.ReturnedQuantity)) return BadRequest(new { error = "Invalid return quantity" });

            saleItem.ReturnedQuantity += item.Quantity;
            saleItem.Product!.Stock += item.Quantity;
            refund += saleItem.UnitPrice * item.Quantity;
        }

        sale.RefundedAmount += refund;
        sale.Total -= refund;
        await _db.SaveChangesAsync();

        // Map response
        var items = sale.Items.Select(i => new SaleItemResponse(i.Id, i.ProductId, i.Product?.Sku ?? string.Empty, i.Product?.Name ?? string.Empty, i.Quantity, i.UnitPrice, i.LineTotal, i.ReturnedQuantity)).ToList();
        var resp = new SaleResponse(sale.Id, sale.Number, sale.CreatedAt, sale.CustomerId, sale.Customer?.FullName, sale.User?.FullName ?? string.Empty, items, sale.Subtotal, sale.Discount, sale.Total, sale.PaidAmount, sale.ChangeAmount, sale.RefundedAmount, sale.PaymentMethod, sale.Status);
        return Ok(resp);
    }
}
