using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public class SettingsController(ISettingsRepo repo, ILogger<SettingsController> logger) : ControllerBase
{
    private readonly ISettingsRepo _repo = repo;
    private readonly ILogger<SettingsController> _logger = logger;

    [HttpGet]
    public async Task<ActionResult<SettingsResponse>> GetSettings()
    {
        var s = await _repo.GetSettingsAsync();
        return Ok(s);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SettingsResponse>> UpdateSettings(UpdateSettingsRequest request)
    {
        var s = await _repo.UpdateSettingsAsync(request);
        _logger.LogInformation("Updated settings");
        return Ok(s);
    }
}
