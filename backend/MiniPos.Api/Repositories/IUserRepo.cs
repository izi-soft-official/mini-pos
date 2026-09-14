using Isopoh.Cryptography.Argon2;
using MiniPos.Api.Models;
using static MiniPos.Api.Dtos.UsersDto;

namespace MiniPos.Api.Repositories
{
    public interface IUserRepo
    {
        Task<IEnumerable<UserResponse>> GetAllUsers();
        Task<UserResponse> CreateUser(CreateUserRequest reqdto);

        Task<bool> UpdateUser(Guid id, UpdateUserRequest request);

        Task<bool> ChangePassword(Guid id, ChangePasswordRequest passdto);

        Task<bool> DeleteUser(Guid id);
        string Hash(string password);
        public void VerifyHash(string password, string hash);
    }
}
