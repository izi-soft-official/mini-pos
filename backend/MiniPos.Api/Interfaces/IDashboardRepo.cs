using MiniPos.Api.Dtos;

namespace MiniPos.Api.Interfaces;

public interface IDashboardRepo
{
    Task<DashboardSummaryResponse> GetSummaryAsync(DateTime from, DateTime to);
    Task<IEnumerable<TopProductResponse>> GetTopProductsAsync(DateTime from, DateTime to, int limit);
    Task<IEnumerable<SalesByDayResponse>> GetSalesByDayAsync(DateTime from, DateTime to);
}
