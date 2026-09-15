using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;

namespace MiniPos.Api.Repositories;

public class DashboardRepo(AppDbContext Db) : IDashboardRepo
{
    private readonly AppDbContext _db = Db;

    public async Task<DashboardSummaryResponse> GetSummaryAsync(DateTime from, DateTime to)
    {
        var sales = await _db.Sales.Where(s => s.CreatedAt >= from && s.CreatedAt <= to).ToListAsync();
        var salesCount = sales.Count;
        var salesTotal = sales.Sum(s => s.Total);
        var average = salesCount > 0 ? salesTotal / salesCount : 0m;
        var itemsSold = await _db.SaleItems.Where(i => i.Sale != null && i.Sale.CreatedAt >= from && i.Sale.CreatedAt <= to).SumAsync(i => i.Quantity);

        var lowStockThreshold = await _db.Settings.Select(s => s.LowStockThreshold).FirstOrDefaultAsync();
        if (lowStockThreshold == 0) lowStockThreshold = 5;
        var lowStockCount = await _db.Products.CountAsync(p => p.Stock <= lowStockThreshold);

        return new DashboardSummaryResponse(salesCount, salesTotal, average, itemsSold, lowStockCount);
    }

    public async Task<IEnumerable<TopProductResponse>> GetTopProductsAsync(DateTime from, DateTime to, int limit)
    {
        var query = await _db.SaleItems
            .Where(i => i.Sale != null && i.Sale.CreatedAt >= from && i.Sale.CreatedAt <= to)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                Total = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(limit)
            .ToListAsync();

        var result = query
            .Join(_db.Products, x => x.ProductId, p => p.Id, (x, p) => new TopProductResponse(p.Id, p.Sku, p.Name, x.Quantity, x.Total))
            .ToList();

        return result;
    }

    public async Task<IEnumerable<SalesByDayResponse>> GetSalesByDayAsync(DateTime from, DateTime to)
    {
        var list = _db.Sales
            .Where(s => s.CreatedAt >= from && s.CreatedAt <= to)
            .AsEnumerable()
            .GroupBy(s => DateOnly.FromDateTime(s.CreatedAt))
            .Select(g => new SalesByDayResponse(g.Key, g.Count(), g.Sum(s => s.Total)))
            .OrderBy(x => x.Date)
            .ToList();

        return list;
    }
}
