using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Interfaces;
using MiniPos.Api.Models;
using static MiniPos.Api.Dtos.UsersDto;
namespace MiniPos.Api.Repositories;

public class UserRepo(AppDbContext db) : IUserRepo
{
    private readonly AppDbContext _db = db;


    public async Task<(IEnumerable<UserResponse> Users, int TotalCount)> GetPagedUsersAsync(string? search, int page, int pageSize)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.Username.Contains(search) ||
                u.FullName.Contains(search)
            );
        }

        int totalCount = await query.CountAsync();

        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserResponse(u.Id, u.Username, u.FullName, u.Role, u.IsActive))
            .ToListAsync();

        return (users, totalCount);
    }

    public async Task<IEnumerable<UserResponse>> GetAllUsers()
    {
        return await _db.Users
            .AsNoTracking()
            .Select(u => new UserResponse(u.Id, u.Username, u.FullName, u.Role, u.IsActive))
            .ToListAsync();
    }

    public async Task<User?> GetEntityByUsernameAsync(string username)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public Task<UserResponse?> GetUserByIdAsync(Guid id)
    {
        return _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(u.Id, u.Username, u.FullName, u.Role, u.IsActive))
            .FirstOrDefaultAsync();
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

        if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            // count total admins currently in the database
            int adminCount = await _db.Users.CountAsync(u => u.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase));

            if (adminCount <= 1)
            {
                return false;
            }
            throw new InvalidOperationException("Cannot delete the last admin user.");
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
