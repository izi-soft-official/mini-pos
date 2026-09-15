namespace MiniPos.Api.Dtos;

public record SaleItemResponse(Guid Id, Guid ProductId, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal, int ReturnedQuantity);

public record SaleResponse(
    Guid Id,
    string Number,
    DateTime CreatedAt,
    Guid? CustomerId,
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

public record SaleListItemResponse(Guid Id, string Number, DateTime CreatedAt, string? CustomerName, int ItemCount, decimal Total, string PaymentMethod, string Status);

public record CreateSaleRequest(Guid? CustomerId, string PaymentMethod, decimal Discount, decimal PaidAmount, List<CreateSaleItemRequest> Items);

public record CreateSaleItemRequest(Guid ProductId, int Quantity, decimal UnitPrice);

public record CreateReturnRequest(string Reason, List<ReturnItemRequest> Items);

public record ReturnItemRequest(Guid SaleItemId, int Quantity);
