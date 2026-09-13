using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;

namespace MiniPos.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<HealthResponse>> Get()
    {
        if (!await _db.Database.CanConnectAsync())
        {
            return StatusCode(503, new { error = "database unreachable" });
        }

        return new HealthResponse
        {
            Status = "ok",
            Database = _db.Database.GetDbConnection().Database,
            Users = await _db.Users.CountAsync()
        };
    }
}
