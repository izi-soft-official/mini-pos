using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;
using static MiniPos.Api.Dtos.UsersDto;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public class AuthController(IUserRepo userRepo, ILogger<AuthController> logger, IAuthRepo authRepo) : ControllerBase
{

    private readonly IUserRepo _userRepo = userRepo;
    private readonly ILogger _logger = logger;
    private readonly IAuthRepo _authRepo = authRepo;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authRepo.LoginAsync(request);
            _logger.LogInformation("Users Logged in Successfully.");
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetCurrentUserInfo()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid token claims." });
        }

        var userResponse = await _userRepo.GetUserByIdAsync(userId);
        if (userResponse == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(userResponse);
    }
}
