namespace MiniPos.Api.Dtos;

public record SaleItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int ReturnedQuantity { get; set; }
}

public record SaleResponse
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public List<SaleItemResponse> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public record SaleListItemResponse
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CustomerName { get; set; }
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public record CreateSaleRequest
{
    public int? CustomerId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Discount { get; set; }
    public decimal PaidAmount { get; set; }
    public List<CreateSaleItemRequest> Items { get; set; } = new();
}

public record CreateSaleItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public record CreateReturnRequest
{
    public string Reason { get; set; } = string.Empty;
    public List<ReturnItemRequest> Items { get; set; } = new();
}

public record ReturnItemRequest
{
    public int SaleItemId { get; set; }
    public int Quantity { get; set; }
}
