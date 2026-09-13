namespace MiniPos.Api.Dtos;

public class SettingsResponse
{
    public string Language { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public int LowStockThreshold { get; set; }
}

public class UpdateSettingsRequest
{
    public string Language { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public int LowStockThreshold { get; set; }
}
