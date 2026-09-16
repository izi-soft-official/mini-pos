using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Models;
using static MiniPos.Api.Dtos.UsersDto;

namespace MiniPos.Api.Interfaces
{
    public interface IUserRepo
    {

        Task<(IEnumerable<UserResponse> Users, int TotalCount)> GetPagedUsersAsync(string? search, int page, int pageSize);
        Task<IEnumerable<UserResponse>> GetAllUsers();
        Task<UserResponse> CreateUser(CreateUserRequest reqdto);

        Task<bool> UpdateUser(Guid id, UpdateUserRequest request);

        Task<bool> ChangePassword(Guid id, ChangePasswordRequest passdto);

        Task<bool> DeleteUser(Guid id);

        Task<User?> GetEntityByUsernameAsync(string username);

        Task<UserResponse?> GetUserByIdAsync(Guid id);

    }
}
