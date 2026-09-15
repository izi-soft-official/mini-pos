using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniPos.Api.Interfaces;
using MiniPos.Api.Models;

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
            if (!exists)
            {
                var userRepo = sp.GetRequiredService<IUserRepo>();
                var passwordHash = userRepo.Hash("izitest");

                // Use raw SQL insert without specifying Id so DB assigns identity/default
                var conn = db.Database.GetDbConnection();
                await conn.OpenAsync();
                try
                {
                    using var insert = conn.CreateCommand();
                    insert.CommandText = "INSERT INTO \"Users\" (\"Username\",\"PasswordHash\",\"FullName\",\"Role\",\"IsActive\") VALUES (@u,@p,@f,@r,@a)";
                    var p1 = insert.CreateParameter(); p1.ParameterName = "@u"; p1.Value = "izitest"; insert.Parameters.Add(p1);
                    var p2 = insert.CreateParameter(); p2.ParameterName = "@p"; p2.Value = passwordHash; insert.Parameters.Add(p2);
                    var p3 = insert.CreateParameter(); p3.ParameterName = "@f"; p3.Value = "izitest"; insert.Parameters.Add(p3);
                    var p4 = insert.CreateParameter(); p4.ParameterName = "@r"; p4.Value = "Admin"; insert.Parameters.Add(p4);
                    var p5 = insert.CreateParameter(); p5.ParameterName = "@a"; p5.Value = true; insert.Parameters.Add(p5);
                    await insert.ExecuteNonQueryAsync();
                    logger.LogInformation("Seeded default user 'izitest' via direct insert.");
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
            else
            {
                logger.LogInformation("Default user 'izitest' already exists.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occured while seeding default user.");
            // do not rethrow to avoid blocking startup
        }
    }
}
