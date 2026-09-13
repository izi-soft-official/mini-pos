using Microsoft.AspNetCore.Identity;
using MiniPos.Api.Models;

namespace MiniPos.Api.Data;

public static class Seed
{
    public static void EnsureTestUser(AppDbContext db)
    {
        if (db.Users.Any())
        {
            return;
        }

        var user = new User
        {
            Username = "admin",
            FullName = "Test Admin",
            Role = "Admin",
            IsActive = true
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "admin123");

        db.Users.Add(user);
        db.SaveChanges();
    }
}
