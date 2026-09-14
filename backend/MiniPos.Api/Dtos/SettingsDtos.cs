namespace MiniPos.Api.Dtos;

public record SettingsResponse
{
    public string Language { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public int LowStockThreshold { get; set; }
}

public record UpdateSettingsRequest
{
    public string Language { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public int LowStockThreshold { get; set; }
}
