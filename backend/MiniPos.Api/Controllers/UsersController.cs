using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private static readonly string[] AllowedRoles = { "Admin", "Manager", "User" };

    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public ActionResult<PagedResponse<UserResponse>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;

        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.Username, pattern)
                                  || EF.Functions.ILike(u.FullName, pattern));
        }

        var total = query.Count();

        var items = query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive
            })
            .ToList();

        return new PagedResponse<UserResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    [HttpPost]
    public ActionResult<UserResponse> CreateUser(CreateUserRequest request)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        if (username.Length == 0)
            return BadRequest(new { error = "Username is required." });

        if (username.Length > 50)
            return BadRequest(new { error = "Username cannot be longer than 50 characters." });

        var fullNameError = ValidateFullName(request.FullName);
        if (fullNameError is not null)
            return BadRequest(new { error = fullNameError });

        var passwordError = ValidatePassword(request.Password);
        if (passwordError is not null)
            return BadRequest(new { error = passwordError });

        var role = ParseRole(request.Role);
        if (role is null)
            return BadRequest(new { error = "Role must be Admin, Manager or User." });

        if (_db.Users.Any(u => u.Username.ToLower() == username.ToLower()))
            return BadRequest(new { error = "This username is already taken." });

        var user = new User
        {
            Username = username,
            FullName = request.FullName.Trim(),
            Role = role,
            IsActive = request.IsActive
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);

        _db.Users.Add(user);
        _db.SaveChanges();

        return StatusCode(StatusCodes.Status201Created, ToResponse(user));
    }

    [HttpPut("{id}")]
    public ActionResult<UserResponse> UpdateUser(int id, UpdateUserRequest request)
    {
        var user = _db.Users.FirstOrDefault(u => u.Id == id);
        if (user is null)
            return NotFound(new { error = "User not found." });

        var fullNameError = ValidateFullName(request.FullName);
        if (fullNameError is not null)
            return BadRequest(new { error = fullNameError });

        var role = ParseRole(request.Role);
        if (role is null)
            return BadRequest(new { error = "Role must be Admin, Manager or User." });

        var removesAdmin = role != "Admin" || !request.IsActive;
        if (removesAdmin && IsLastActiveAdmin(user))
            return BadRequest(new { error = "This is the last active admin. Make another user an active admin first." });

        user.FullName = request.FullName.Trim();
        user.Role = role;
        user.IsActive = request.IsActive;

        _db.SaveChanges();

        return ToResponse(user);
    }

    [HttpPut("{id}/password")]
    public IActionResult ChangePassword(int id, ChangePasswordRequest request)
    {
        var user = _db.Users.FirstOrDefault(u => u.Id == id);
        if (user is null)
            return NotFound(new { error = "User not found." });

        var passwordError = ValidatePassword(request.NewPassword);
        if (passwordError is not null)
            return BadRequest(new { error = passwordError });

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.NewPassword);
        _db.SaveChanges();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteUser(int id)
    {
        var user = _db.Users.FirstOrDefault(u => u.Id == id);
        if (user is null)
            return NotFound(new { error = "User not found." });

        if (_db.Sales.Any(s => s.UserId == id))
            return BadRequest(new { error = "This user has sales. Deactivate them instead of deleting." });

        if (IsLastActiveAdmin(user))
            return BadRequest(new { error = "This is the last active admin. Make another user an active admin first." });

        _db.Users.Remove(user);
        _db.SaveChanges();

        return NoContent();
    }

    private bool IsLastActiveAdmin(User user) =>
        user.Role == "Admin"
        && user.IsActive
        && !_db.Users.Any(u => u.Id != user.Id && u.Role == "Admin" && u.IsActive);

    private static string? ValidateFullName(string? fullName)
    {
        var trimmed = fullName?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            return "Full name is required.";

        if (trimmed.Length > 120)
            return "Full name cannot be longer than 120 characters.";

        return null;
    }

    private static string? ValidatePassword(string? password)
    {
        if (password is null || password.Length < 6)
            return "Password must be at least 6 characters.";

        return null;
    }

    private static string? ParseRole(string? role) =>
        AllowedRoles.FirstOrDefault(r => string.Equals(r, role?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static UserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        FullName = user.FullName,
        Role = user.Role,
        IsActive = user.IsActive
    };
}
