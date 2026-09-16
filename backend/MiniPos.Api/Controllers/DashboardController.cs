using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/dashboard")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var fromDt = from.HasValue ? from.Value.ToDateTime(new TimeOnly(0)) : DateTime.MinValue;
        var toDt = to.HasValue ? to.Value.ToDateTime(new TimeOnly(23, 59, 59)) : DateTime.MaxValue;

        var salesQuery = _db.Sales.Where(s => s.CreatedAt >= fromDt && s.CreatedAt <= toDt);
        var salesCount = await salesQuery.CountAsync();
        var salesTotal = await salesQuery.SumAsync(s => (decimal?)s.Total) ?? 0m;
        var averageSale = salesCount > 0 ? salesTotal / salesCount : 0m;
        var itemsSold = await _db.SaleItems.Where(si => si.Sale != null && si.Sale.CreatedAt >= fromDt && si.Sale.CreatedAt <= toDt).SumAsync(si => (int?)si.Quantity) ?? 0;
        var lowStockThreshold = await _db.Settings.Select(s => (int?)s.LowStockThreshold).FirstOrDefaultAsync() ?? 5;
        var lowStockCount = await _db.Products.CountAsync(p => p.Stock <= lowStockThreshold);

        var resp = new DashboardSummaryResponse(salesCount, salesTotal, averageSale, itemsSold, lowStockCount);
        return Ok(resp);
    }

    [HttpGet("top-products")]
    public async Task<ActionResult<List<TopProductResponse>>> GetTopProducts(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int limit = 5)
    {
        var fromDt = from.HasValue ? from.Value.ToDateTime(new TimeOnly(0)) : DateTime.MinValue;
        var toDt = to.HasValue ? to.Value.ToDateTime(new TimeOnly(23, 59, 59)) : DateTime.MaxValue;

        var list = await _db.SaleItems
            .Where(si => si.Sale != null && si.Sale.CreatedAt >= fromDt && si.Sale.CreatedAt <= toDt)
            .GroupBy(si => si.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity), Total = g.Sum(x => x.LineTotal) })
            .OrderByDescending(x => x.Quantity)
            .Take(limit)
            .Join(_db.Products, x => x.ProductId, p => p.Id, (x, p) => new TopProductResponse(p.Id, p.Sku, p.Name, x.Quantity, x.Total))
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("sales-by-day")]
    public async Task<ActionResult<List<SalesByDayResponse>>> GetSalesByDay(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var fromDt = from.HasValue ? from.Value.ToDateTime(new TimeOnly(0)) : DateTime.MinValue;
        var toDt = to.HasValue ? to.Value.ToDateTime(new TimeOnly(23, 59, 59)) : DateTime.MaxValue;

        var list = await _db.Sales
            .Where(s => s.CreatedAt >= fromDt && s.CreatedAt <= toDt)
            .GroupBy(s => DateOnly.FromDateTime(s.CreatedAt))
            .Select(g => new SalesByDayResponse(g.Key, g.Count(), g.Sum(s => s.Total)))
            .OrderBy(x => x.Date)
            .ToListAsync();

        return Ok(list);
    }
}
