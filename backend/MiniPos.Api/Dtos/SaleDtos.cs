namespace MiniPos.Api.Dtos;

public record SaleItemResponse(int Id, int ProductId, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal, int ReturnedQuantity);

public record SaleResponse(
    int Id,
    string Number,
    DateTime CreatedAt,
    int? CustomerId,
    string? CustomerName,
    string CashierName,
    List<SaleItemResponse> Items,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    decimal PaidAmount,
    decimal ChangeAmount,
    decimal RefundedAmount,
    string PaymentMethod,
    string Status
);

public record SaleListItemResponse(int Id, string Number, DateTime CreatedAt, string? CustomerName, int ItemCount, decimal Total, string PaymentMethod, string Status);

public record CreateSaleRequest(int? CustomerId, string PaymentMethod, decimal Discount, decimal PaidAmount, List<CreateSaleItemRequest> Items);

public record CreateSaleItemRequest(int ProductId, int Quantity, decimal UnitPrice);

public record CreateReturnRequest(string Reason, List<ReturnItemRequest> Items);

public record ReturnItemRequest(int SaleItemId, int Quantity);
