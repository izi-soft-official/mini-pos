namespace MiniPos.Api.Dtos;

public record DashboardSummaryResponse
{
    public int SalesCount { get; set; }
    public decimal SalesTotal { get; set; }
    public decimal AverageSale { get; set; }
    public int ItemsSold { get; set; }
    public int LowStockCount { get; set; }
}

public record TopProductResponse
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Total { get; set; }
}

public record SalesByDayResponse
{
    public DateOnly Date { get; set; }
    public int SalesCount { get; set; }
    public decimal Total { get; set; }
}
