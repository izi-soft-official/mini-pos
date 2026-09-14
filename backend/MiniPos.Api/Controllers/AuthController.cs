using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using static MiniPos.Api.Dtos.UsersDto;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public class AuthController(AppDbContext db, IConfiguration configuration) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly IConfiguration _configuration = configuration;

    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpGet("me")]
    public ActionResult<UserResponse> Me()
    {
        throw new NotImplementedException();
    }
}
