using MiniPos.Api.Models;

namespace MiniPos.Api.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);

}
