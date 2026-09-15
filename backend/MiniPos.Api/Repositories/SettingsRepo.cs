using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;
using MiniPos.Api.Models;

namespace MiniPos.Api.Repositories;

public class SettingsRepo(AppDbContext Db) : ISettingsRepo
{
    private readonly AppDbContext _db = Db;

    public async Task<SettingsResponse> GetSettingsAsync()
    {
        var s = await _db.Settings.FirstOrDefaultAsync();
        if (s == null) return new SettingsResponse("en", "light", 5);
        return new SettingsResponse(s.Language, s.Theme, s.LowStockThreshold);
    }

    public async Task<SettingsResponse> UpdateSettingsAsync(UpdateSettingsRequest request)
    {
        var s = await _db.Settings.FirstOrDefaultAsync();
        if (s == null)
        {
            s = new Setting { Id = Guid.NewGuid(), Language = request.Language, Theme = request.Theme, LowStockThreshold = request.LowStockThreshold };
            _db.Settings.Add(s);
        }
        else
        {
            s.Language = request.Language;
            s.Theme = request.Theme;
            s.LowStockThreshold = request.LowStockThreshold;
        }

        await _db.SaveChangesAsync();
        return new SettingsResponse(s.Language, s.Theme, s.LowStockThreshold);
    }
}
