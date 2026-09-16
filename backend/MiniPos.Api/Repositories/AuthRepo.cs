using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;

namespace MiniPos.Api.Repositories;

public class AuthRepo(IUserRepo userRepo, ITokenService tokenService,IHashingService hashingService) : IAuthRepo
{
    private readonly ITokenService _tokenService = tokenService;
    private readonly IUserRepo _userRepo = userRepo;
    private readonly IHashingService _hashingService = hashingService;
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetEntityByUsernameAsync(request.Username);
        if (user == null || !_hashingService.VerifyHash(user.PasswordHash, request.Password))
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

