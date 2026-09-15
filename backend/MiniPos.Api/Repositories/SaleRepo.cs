using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;
using MiniPos.Api.Models;

namespace MiniPos.Api.Repositories;

public class SaleRepo(AppDbContext Db) : ISaleRepo
{
    private readonly AppDbContext _db = Db;

    public async Task<SaleResponse> CreateSaleAsync(CreateSaleRequest request, Guid? userId)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new InvalidOperationException("No items provided");

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var it in request.Items)
            {
                var product = await _db.Products.FindAsync(it.ProductId);
                if (product == null) throw new InvalidOperationException($"Product {it.ProductId} not found");
                if (product.Stock < it.Quantity) throw new InvalidOperationException($"Insufficient stock for product {product.Name}");
            }

            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                Number = $"S-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                CreatedAt = DateTime.UtcNow,
                CustomerId = request.CustomerId,
                PaymentMethod = request.PaymentMethod,
                Discount = request.Discount,
                PaidAmount = request.PaidAmount,
                Status = "Completed",
                UserId = userId
            };

            decimal subtotal = 0m;
            foreach (var it in request.Items)
            {
                var product = await _db.Products.FindAsync(it.ProductId)!;
                product.Stock -= it.Quantity;
                var line = new SaleItem
                {
                    Id = Guid.NewGuid(),
                    SaleId = sale.Id,
                    ProductId = it.ProductId,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    LineTotal = it.UnitPrice * it.Quantity,
                    ReturnedQuantity = 0
                };
                subtotal += line.LineTotal;
                sale.Items.Add(line);
            }

            sale.Subtotal = subtotal;
            sale.Total = subtotal - sale.Discount;
            sale.ChangeAmount = sale.PaidAmount - sale.Total;

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return await GetByIdAsync(sale.Id) ?? throw new InvalidOperationException("Failed to retrieve created sale");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(IEnumerable<SaleListItemResponse> Items, int Total)> GetPagedSalesAsync(DateTime from, DateTime to, Guid? customerId, string? status, int page, int pageSize)
    {
        var query = _db.Sales.AsQueryable().Where(s => s.CreatedAt >= from && s.CreatedAt <= to);
        if (customerId.HasValue) query = query.Where(s => s.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SaleListItemResponse(s.Id, s.Number, s.CreatedAt, s.Customer != null ? s.Customer.FullName : null, s.Items.Count, s.Total, s.PaymentMethod, s.Status))
            .ToListAsync();

        return (items, total);
    }

    public async Task<SaleResponse?> GetByIdAsync(Guid id)
    {
        var s = await _db.Sales
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)
            .Include(x => x.Customer)
            .Include(x => x.User)
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (s == null) return null;

        return new SaleResponse(
            s.Id,
            s.Number,
            s.CreatedAt,
            s.CustomerId,
            s.Customer != null ? s.Customer.FullName : null,
            s.User != null ? s.User.FullName : string.Empty,
            s.Items.Select(i => new SaleItemResponse(i.Id, i.ProductId, i.Product != null ? i.Product.Sku : string.Empty, i.Product != null ? i.Product.Name : string.Empty, i.Quantity, i.UnitPrice, i.LineTotal, i.ReturnedQuantity)).ToList(),
            s.Subtotal,
            s.Discount,
            s.Total,
            s.PaidAmount,
            s.ChangeAmount,
            s.RefundedAmount,
            s.PaymentMethod,
            s.Status
        );
    }

    public async Task<SaleResponse> ReturnSaleAsync(Guid id, CreateReturnRequest request)
    {
        var sale = await _db.Sales.Include(s => s.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null) throw new InvalidOperationException("Sale not found");

        foreach (var item in request.Items)
        {
            var saleItem = sale.Items.FirstOrDefault(i => i.Id == item.SaleItemId);
            if (saleItem == null) throw new InvalidOperationException($"Sale item {item.SaleItemId} not found");
            if (saleItem.ReturnedQuantity + item.Quantity > saleItem.Quantity) throw new InvalidOperationException("Return quantity exceeds sold quantity");

            saleItem.ReturnedQuantity += item.Quantity;
            if (saleItem.Product != null) saleItem.Product.Stock += item.Quantity;
            sale.RefundedAmount += saleItem.UnitPrice * item.Quantity;
        }

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(sale.Id))!;
    }
}
