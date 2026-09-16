using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public class SettingsController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<ActionResult<SettingsResponse>> GetSettings()
    {
        var s = await _db.Settings.FirstOrDefaultAsync();
        if (s == null) return NotFound(new { error = "Settings not found." });
        var resp = new SettingsResponse(s.Language, s.Theme, s.LowStockThreshold);
        return Ok(resp);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SettingsResponse>> UpdateSettings(UpdateSettingsRequest request)
    {
        var s = await _db.Settings.FirstOrDefaultAsync();
        if (s == null)
        {
            s = new Setting { Language = request.Language, Theme = request.Theme, LowStockThreshold = request.LowStockThreshold };
            await _db.Settings.AddAsync(s);
        }
        else
        {
            s.Language = request.Language;
            s.Theme = request.Theme;
            s.LowStockThreshold = request.LowStockThreshold;
            _db.Settings.Update(s);
        }

        await _db.SaveChangesAsync();
        return Ok(new SettingsResponse(s.Language, s.Theme, s.LowStockThreshold));
    }
}
