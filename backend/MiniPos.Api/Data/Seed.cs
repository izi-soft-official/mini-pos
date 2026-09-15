using MiniPos.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace MiniPos.Api.Data;

public static class Seed
{
    public static void EnsureTestUser(AppDbContext db)
    {
        if (db.Users.Any())
            return;

        var user = new User
        {
            Username = "admin",
            FullName = "Test Admin",
            Role = "Admin",
            IsActive = true
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "admin123");

        db.Users.Add(user);
        db.SaveChangesAsync();
    }

    public static void EnsureDemoData(AppDbContext db)
    {
        EnsureSettings(db);
        EnsureCatalog(db);
        EnsureCustomers(db);
    }

    private static void EnsureSettings(AppDbContext db)
    {
        if (db.Settings.Any())
            return;

        db.Settings.Add(new Setting
        {
            Id = 1,
            Language = "en",
            Theme = "light",
            LowStockThreshold = 5
        });

        db.SaveChanges();
    }

    private static void EnsureCatalog(AppDbContext db)
    {
        if (db.Categories.Any())
            return;

        var snacks = new Category { Name = "Snacks" };
        var drinks = new Category { Name = "Drinks" };

        db.Categories.AddRange(snacks, drinks);
        db.SaveChanges();

        db.Products.AddRange(
            new Product { Sku = "SNK-001", Name = "Kool", CategoryId = snacks.Id, Price = 30m, Cost = 20m, Stock = 20 },
            new Product { Sku = "SNK-002", Name = "Kool 4 Winners", CategoryId = snacks.Id, Price = 70m, Cost = 50m, Stock = 20 },
            new Product { Sku = "SNK-003", Name = "Kool 9 Winners", CategoryId = snacks.Id, Price = 120m, Cost = 80m, Stock = 20 },
            new Product { Sku = "DRK-001", Name = "IZEM 33CL", CategoryId = drinks.Id, Price = 70m, Cost = 50m, Stock = 30 },
            new Product { Sku = "DRK-002", Name = "IZEM 24CL", CategoryId = drinks.Id, Price = 100m, Cost = 70m, Stock = 30 },
            new Product { Sku = "DRK-003", Name = "IZEM 50CL", CategoryId = drinks.Id, Price = 150m, Cost = 100m, Stock = 30 }
        );

        db.SaveChanges();
    }

    private static void EnsureCustomers(AppDbContext db)
    {
        if (db.Customers.Any())
            return;

        var now = DateTime.UtcNow;

        db.Customers.AddRange(
            new Customer { FullName = "Faycal", Phone = "", Email = "", Note = "", CreatedAt = now },
            new Customer { FullName = "Yasser", Phone = "", Email = "", Note = "", CreatedAt = now },
            new Customer { FullName = "Amine", Phone = "", Email = "", Note = "", CreatedAt = now }
            );

        db.SaveChanges();
    }

}
