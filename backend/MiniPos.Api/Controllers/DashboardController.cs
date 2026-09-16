using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public ActionResult<DashboardSummaryResponse> GetSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var (first, last) = ResolveRange(from, to);
        if (first > last)
            return BadRequest(new { error = "The start date must be on or before the end date." });

        var start = StartOf(first);
        var end = StartOf(last.AddDays(1));

        var sales = _db.Sales.Where(s => s.CreatedAt >= start && s.CreatedAt < end);

        var salesCount = sales.Count();
        var salesTotal = sales.Sum(s => s.Total - s.RefundedAmount);

        var itemsSold = _db.SaleItems
            .Where(i => i.Sale!.CreatedAt >= start && i.Sale.CreatedAt < end)
            .Sum(i => i.Quantity - i.ReturnedQuantity);

        var threshold = (_db.Settings.FirstOrDefault(s => s.Id == 1) ?? new Setting()).LowStockThreshold;
        var lowStockCount = _db.Products.Count(p => p.IsActive && p.Stock <= threshold);

        return new DashboardSummaryResponse
        {
            SalesCount = salesCount,
            SalesTotal = salesTotal,
            AverageSale = salesCount == 0 ? 0m : Math.Round(salesTotal / salesCount, 3),
            ItemsSold = itemsSold,
            LowStockCount = lowStockCount
        };
    }

    [HttpGet("top-products")]
    public ActionResult<List<TopProductResponse>> GetTopProducts(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int limit = 5)
    {
        var (first, last) = ResolveRange(from, to);
        if (first > last)
            return BadRequest(new { error = "The start date must be on or before the end date." });

        limit = limit < 1 ? 5 : limit > 50 ? 50 : limit;

        var start = StartOf(first);
        var end = StartOf(last.AddDays(1));

        var rows = _db.SaleItems
            .Where(i => i.Sale!.CreatedAt >= start && i.Sale.CreatedAt < end)
            .GroupBy(i => new { i.ProductId, i.Product!.Sku, i.Product.Name })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Sku,
                g.Key.Name,
                QuantitySold = g.Sum(i => i.Quantity - i.ReturnedQuantity),
                Total = g.Sum(i => (i.Quantity - i.ReturnedQuantity) * i.UnitPrice)
            })
            .Where(p => p.QuantitySold > 0)
            .OrderByDescending(p => p.QuantitySold)
            .ThenBy(p => p.Name)
            .Take(limit)
            .ToList();

        return rows.Select(p => new TopProductResponse
        {
            ProductId = p.ProductId,
            Sku = p.Sku,
            Name = p.Name,
            QuantitySold = p.QuantitySold,
            Total = p.Total
        }).ToList();
    }

    [HttpGet("sales-by-day")]
    public ActionResult<List<SalesByDayResponse>> GetSalesByDay(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var (first, last) = ResolveRange(from, to);
        if (first > last)
            return BadRequest(new { error = "The start date must be on or before the end date." });

        var start = StartOf(first);
        var end = StartOf(last.AddDays(1));

        var byDay = _db.Sales
            .Where(s => s.CreatedAt >= start && s.CreatedAt < end)
            .Select(s => new { s.CreatedAt, Net = s.Total - s.RefundedAmount })
            .ToList()
            .GroupBy(s => DateOnly.FromDateTime(s.CreatedAt))
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Total: g.Sum(s => s.Net)));

        var result = new List<SalesByDayResponse>();
        for (var day = first; day <= last; day = day.AddDays(1))
        {
            byDay.TryGetValue(day, out var totals);
            result.Add(new SalesByDayResponse
            {
                Date = day,
                SalesCount = totals.Count,
                Total = totals.Total
            });
        }

        return result;
    }

    private static (DateOnly First, DateOnly Last) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var last = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return (from ?? last, last);
    }

    private static DateTime StartOf(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
}
