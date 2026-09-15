using MiniPos.Api.Dtos;

namespace MiniPos.Api.Interfaces;

public interface ISaleRepo
{
    Task<(IEnumerable<SaleListItemResponse> Items, int Total)> GetPagedSalesAsync(DateTime from, DateTime to, Guid? customerId, string? status, int page, int pageSize);
    Task<SaleResponse?> GetByIdAsync(Guid id);
    Task<SaleResponse> CreateSaleAsync(CreateSaleRequest request, Guid? userId);
    Task<SaleResponse> ReturnSaleAsync(Guid id, CreateReturnRequest request);
}
