using System.Security.Claims;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;
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
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !Argon2.Verify(user.PasswordHash, request.Password))
        {
            return Unauthorized(new { error = "Invalid username or password." });
        }

        var (token, expiresAt) = GenerateToken(user);
        var userDto = new UserResponse(user.Id, user.Username, user.FullName, user.Role, user.IsActive);

        return Ok(new LoginResponse(token, expiresAt, userDto));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetCurrentUserInfo()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token claims." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found." });
        }

        return Ok(new UserResponse(user.Id, user.Username, user.FullName, user.Role, user.IsActive));
    }

    private (string, DateTime) GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not found in configuration.");
        var key = System.Text.Encoding.UTF8.GetBytes(secretKey);

        var claims = new List<Claim>
        {
            new (System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new (System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName, user.Username),
            new (ClaimTypes.Role, user.Role),
            new (ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var expiresAt = DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpiryHours"] ?? "2"));

        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), expiresAt);
    }
}
