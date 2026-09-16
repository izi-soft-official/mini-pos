using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public class UsersController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<ActionResult> GetUsers(
    [FromQuery] string? search,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
    {
        try
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            var query = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search}%";
                query = query.Where(u => EF.Functions.ILike(u.Username, pattern) || EF.Functions.ILike(u.FullName, pattern));
            }

            var totalCount = await query.CountAsync();
            var users = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(u => new UsersDto.UserResponse(u.Id, u.Username, u.FullName, u.Role, u.IsActive))
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                data = users
            });
        }
        catch (Exception)
        {
            return StatusCode(503, new { error = "Error From The Database" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<UsersDto.UserResponse>> CreateUser([FromBody] UsersDto.CreateUserRequest request)
    {
        try
        {
            if (await _db.Users.AnyAsync(u => u.Username == request.Username))
                throw new InvalidOperationException("Username already exists");

            var user = new User
            {
                Username = request.Username,
                FullName = request.FullName,
                PasswordHash = Argon2.Hash(request.Password),
                Role = request.Role,
                IsActive = request.IsActive
            };
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();
            var dto = new UsersDto.UserResponse(user.Id, user.Username, user.FullName, user.Role, user.IsActive);
            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, dto);

        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateUser(int id, [FromBody] UsersDto.UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound(new { error = "User not found." });
        user.FullName = request.FullName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "User Updated Successfully." });
    }

    [HttpPut("{id:int}/password")]
    public async Task<ActionResult> ChangePassword(int id, [FromBody] UsersDto.ChangePasswordRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound(new { error = "User not found." });
        if (!Argon2.Verify(user.PasswordHash, request.OldPassword))
            return BadRequest(new { error = "Failed to update Password, Check the Current Password" });

        user.PasswordHash = Argon2.Hash(request.NewPassword);
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Password Changed Successfully." });

    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound(new { error = "Failed to delete user. User may not exist." });
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "User deleted successfully." });
    }
}
