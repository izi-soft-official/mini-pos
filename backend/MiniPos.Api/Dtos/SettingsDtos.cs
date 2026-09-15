namespace MiniPos.Api.Dtos;

public record SettingsResponse(string Language, string Theme, int LowStockThreshold);

public record UpdateSettingsRequest(string Language, string Theme, int LowStockThreshold);
