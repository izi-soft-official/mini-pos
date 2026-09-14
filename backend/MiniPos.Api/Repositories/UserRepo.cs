using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using Isopoh.Cryptography.Argon2;
using MiniPos.Api.Models;
using static MiniPos.Api.Dtos.UsersDto;
namespace MiniPos.Api.Repositories;

public class UserRepo(AppDbContext db) : IUserRepo
{
    private readonly AppDbContext _db = db;

    public async Task<IEnumerable<UserResponse>> GetAllUsers()
    {
        return await _db.Users
            .AsNoTracking()
            .Select(u => new UserResponse(u.Id, u.Username, u.FullName, u.Role, u.IsActive))
            .ToListAsync();
    }



    public async Task<UserResponse> CreateUser(CreateUserRequest reqdto)
    {
        bool exist = await _db.Users.AnyAsync(u => u.Username == reqdto.Username || u.FullName == reqdto.FullName);


        if (exist)
        {
            throw new InvalidOperationException("Username or Full Name already Exists");
        }
        var user = new User
        {
            Username = reqdto.Username,
            FullName = reqdto.FullName,
            PasswordHash = Hash(reqdto.Password),
            Role = reqdto.Role,
            IsActive = reqdto.IsActive
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();


        return new UserResponse(user.Id, user.Username, user.FullName, user.Role, user.IsActive);
    }



    public async Task<bool> UpdateUser(Guid id, UpdateUserRequest request)
    {

        var taken = await _db.Users.AnyAsync(u => u.Id != id && (u.FullName == request.FullName));

        if (taken)
        {
            return false;
            throw new InvalidOperationException("User with the same full name already exists");
        }


        var user = await _db.Users.FindAsync(id);

        if (user == null)
        {
            return false;
            throw new InvalidOperationException("no User was Found.");
        }

        user.FullName = request.FullName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePassword(Guid id, ChangePasswordRequest passdto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return false;
            throw new InvalidOperationException("no User was Found.");
        }
        if (!Argon2.Verify(user.PasswordHash, passdto.OldPassword))
        {
            return false;
            throw new InvalidOperationException("Old password is incorrect.");
        }

        user.PasswordHash = Hash(passdto.NewPassword);

        await _db.SaveChangesAsync();
        return true;
    }


    public async Task<bool> DeleteUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return false;
            throw new InvalidOperationException("no User was Found.");
        }
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }


    public string Hash(string password)
    {
        return Argon2.Hash(password);
    }

    public void VerifyHash(string password, string hash)
    {
        Argon2.Verify(password, hash);
    }
}
