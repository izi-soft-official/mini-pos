using MiniPos.Api.Dtos;

namespace MiniPos.Api.Interfaces
{
    public interface IAuthRepo
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
    }
}
