using MiniPos.Api.Dtos;

namespace MiniPos.Api.Interfaces;

public interface ISettingsRepo
{
    Task<SettingsResponse> GetSettingsAsync();
    Task<SettingsResponse> UpdateSettingsAsync(UpdateSettingsRequest request);
}
