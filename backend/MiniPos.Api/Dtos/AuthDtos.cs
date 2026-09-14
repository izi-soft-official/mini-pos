using static MiniPos.Api.Dtos.UsersDto;

namespace MiniPos.Api.Dtos;

public record LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(string Token, DateTime ExpiredAt, UserResponse User);
