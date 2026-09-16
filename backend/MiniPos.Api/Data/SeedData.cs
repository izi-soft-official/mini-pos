using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Interfaces;

namespace MiniPos.Api.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("SeedData");
        try
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            var exists = await db.Users.AnyAsync(u => u.Username == "izitest");
            var settingsExists = await db.Settings.AnyAsync();

            if (!exists)
            {
                try
                {
                    var userRepo = sp.GetRequiredService<IUserRepo>();
                    var createReq = new MiniPos.Api.Dtos.UsersDto.CreateUserRequest
                    {
                        Username = "izitest",
                        FullName = "izitest",
                        Password = "izitest",
                        Role = "Admin",
                        IsActive = true
                    };

                    await userRepo.CreateUser(createReq);
                    logger.LogInformation("Seeded default user 'izitest'.");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to seed default user via repository; trying direct insert.");
                    var passwordHash = sp.GetRequiredService<IHashingService>().Hash("izitest");
                    db.Users.Add(new Models.User { Username = "izitest", PasswordHash = passwordHash, FullName = "izitest", Role = "Admin", IsActive = true });
                    await db.SaveChangesAsync();
                    logger.LogInformation("Seeded default user 'izitest' via direct insert fallback.");
                }
            }

            if (!settingsExists)
            {
                db.Settings.Add(new Models.Setting { Id = Guid.NewGuid(), Language = "en", Theme = "light", LowStockThreshold = 5 });
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded default settings.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occured while seeding default user and settings");
        }
    }
}
