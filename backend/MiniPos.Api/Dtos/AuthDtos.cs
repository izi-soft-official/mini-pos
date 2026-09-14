using static MiniPos.Api.Dtos.UsersDto;

namespace MiniPos.Api.Dtos;

public record LoginRequest(string Username, string Password);


public record LoginResponse(string Token, DateTime ExpiresAt, UserResponse User);
