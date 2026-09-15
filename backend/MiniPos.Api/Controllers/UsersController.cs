using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Interfaces;
using static MiniPos.Api.Dtos.UsersDto;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public class UsersController(IUserRepo userRepo, ILogger<UsersController> logger) : ControllerBase
{
    private readonly IUserRepo _userRepo = userRepo;
    private readonly ILogger<UsersController> _logger = logger;

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

            var (users, totalCount) = await _userRepo.GetPagedUsersAsync(search, page, pageSize);

            _logger.LogInformation("Got the paged users successfully.");

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                data = users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed trying to retrieve users.");
            return StatusCode(503, new { message = "Error From The Database" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _userRepo.CreateUser(request);
            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);

        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create user due to validation error");
            return BadRequest(new { ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var updateSuccess = await _userRepo.UpdateUser(id, request);
        _logger.LogInformation("User updated successfully");
        if (!updateSuccess)
        {
            return Conflict(new { Message = "Failed to update user. User may not exist or data is invalid." });
        }
        return Ok(new { Message = "User Updated Successfully." });
    }

    [HttpPut("{id:guid}/password")]
    public async Task<ActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        var updateSuccess = await _userRepo.ChangePassword(id, request);
        _logger.LogInformation("Password changed successfully");

        if (!updateSuccess)
        {
            return BadRequest(new { Message = "Failed to update Password, Check the Current Password" });
        }


        return Ok(new { Message = "Password Changed Successfully." });

    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        var RemovedUser = await _userRepo.DeleteUser(id);
        _logger.LogInformation("User deleted successfully");

        if (!RemovedUser)
        {
            return NotFound(new { Message = "Failed to delete user. User may not exist." });
        }
        return Ok(new { Message = "User deleted successfully." });
    }
}
