namespace MiniPos.Api.Dtos;

// ============================================================
// EXISTING PARSE-SALE DTOs
// ============================================================

public class ParseSaleRequest
{
    public string Text { get; set; } = string.Empty;
}

public class ParsedSaleItem
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public int AvailableStock { get; set; }
    public bool InsufficientStock { get; set; }
}

public class ParseSaleResponse
{
    public List<ParsedSaleItem> Items { get; set; } = new();
    public List<string> Unmatched { get; set; } = new();
    public List<string> StockWarnings { get; set; } = new();
}


// ============================================================
// AI ASSISTANT DTOs
// ============================================================

public class AiAssistantRequest
{
    public string Text { get; set; } = string.Empty;
}

public class AiAssistantResponse
{
    public string Intent { get; set; } = "unknown";

    public string Message { get; set; } = string.Empty;

    public bool RequiresConfirmation { get; set; }

    public bool SaleCreated { get; set; }

    public int? SaleId { get; set; }

    public string? SaleNumber { get; set; }

    public AiSalePreview? Preview { get; set; }

    public List<string> Unmatched { get; set; } = new();

    public List<string> Warnings { get; set; } = new();
}

public class AiSalePreview
{
    public int? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public string PaymentMethod { get; set; } = "cash";

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Total { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    public List<AiSalePreviewItem> Items { get; set; } = new();
}

public class AiSalePreviewItem
{
    public int ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public int AvailableStock { get; set; }

    public bool InsufficientStock { get; set; }
}