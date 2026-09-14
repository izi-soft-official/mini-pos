using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;
using BCrypt.Net;

namespace MiniPos.Api.Repositories;

public class AuthRepo(IUserRepo userRepo, ITokenService tokenService) : IAuthRepo
{
    private readonly ITokenService _tokenService = tokenService;
    private readonly IUserRepo _userRepo = userRepo;
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetEntityByUsernameAsync(request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is disabled.");
        }

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        var userResponse = new UsersDto.UserResponse(
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.IsActive
        );

        return new LoginResponse(token, expiresAt, userResponse);
    }
}

