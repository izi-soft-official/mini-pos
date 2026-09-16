using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;

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
                    var passwordHash = Argon2.Hash("izitest");
                    db.Users.Add(new Models.User { Username = "izitest", PasswordHash = passwordHash, FullName = "izitest", Role = "Admin", IsActive = true});
                    await db.SaveChangesAsync();
                }
                catch
                {
                }
            }

            if (!settingsExists)
            {
                db.Settings.Add(new Models.Setting { Language = "en", Theme = "light", LowStockThreshold = 5 });
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
