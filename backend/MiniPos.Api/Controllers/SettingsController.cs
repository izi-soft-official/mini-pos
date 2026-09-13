using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public ActionResult<SettingsResponse> GetSettings()
    {
        throw new NotImplementedException();
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public ActionResult<SettingsResponse> UpdateSettings(UpdateSettingsRequest request)
    {
        throw new NotImplementedException();
    }
}
