using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private static readonly string[] Languages = { "en", "fr", "ar" };
    private static readonly string[] Themes = { "light", "dark" };

    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public ActionResult<SettingsResponse> GetSettings()
    {
        return ToResponse(LoadSettings());
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public ActionResult<SettingsResponse> UpdateSettings(UpdateSettingsRequest request)
    {
        var language = request.Language?.Trim().ToLower() ?? string.Empty;
        if (!Languages.Contains(language))
            return BadRequest(new { error = "Language must be en, fr or ar." });

        var theme = request.Theme?.Trim().ToLower() ?? string.Empty;
        if (!Themes.Contains(theme))
            return BadRequest(new { error = "Theme must be light or dark." });

        if (request.LowStockThreshold < 0)
            return BadRequest(new { error = "Low stock threshold cannot be negative." });

        var setting = LoadSettings();
        setting.Language = language;
        setting.Theme = theme;
        setting.LowStockThreshold = request.LowStockThreshold;

        _db.SaveChanges();

        return ToResponse(setting);
    }

    private Setting LoadSettings()
    {
        var setting = _db.Settings.FirstOrDefault(s => s.Id == 1);
        if (setting is not null)
            return setting;

        setting = new Setting { Id = 1 };
        _db.Settings.Add(setting);
        _db.SaveChanges();

        return setting;
    }

    private static SettingsResponse ToResponse(Setting setting) => new()
    {
        Language = setting.Language,
        Theme = setting.Theme,
        LowStockThreshold = setting.LowStockThreshold
    };
}
