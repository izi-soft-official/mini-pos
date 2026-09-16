namespace MiniPos.Api.Dtos;

public record DashboardSummaryResponse(int SalesCount, decimal SalesTotal, decimal AverageSale, int ItemsSold, int LowStockCount);

public record TopProductResponse(int ProductId, string Sku, string Name, int QuantitySold, decimal Total);

public record SalesByDayResponse(DateOnly Date, int SalesCount, decimal Total);
