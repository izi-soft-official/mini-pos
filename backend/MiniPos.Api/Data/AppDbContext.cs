using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Models;

namespace MiniPos.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(120).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(20).IsRequired();
            entity.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(80).IsRequired();
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Sku).HasMaxLength(40).IsRequired();
            entity.Property(p => p.Name).HasMaxLength(150).IsRequired();
            entity.Property(p => p.Price).HasColumnType("numeric(14,3)");
            entity.Property(p => p.Cost).HasColumnType("numeric(14,3)");
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.FullName).HasMaxLength(120).IsRequired();
            entity.Property(c => c.Phone).HasMaxLength(40);
            entity.Property(c => c.Email).HasMaxLength(120);
            entity.Property(c => c.Note).HasMaxLength(500);
            entity.HasIndex(c => c.FullName);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(s => s.Number).HasMaxLength(20).IsRequired();
            entity.Property(s => s.PaymentMethod).HasMaxLength(20).IsRequired();
            entity.Property(s => s.Status).HasMaxLength(20).IsRequired();
            entity.Property(s => s.Subtotal).HasColumnType("numeric(14,3)");
            entity.Property(s => s.Discount).HasColumnType("numeric(14,3)");
            entity.Property(s => s.Total).HasColumnType("numeric(14,3)");
            entity.Property(s => s.PaidAmount).HasColumnType("numeric(14,3)");
            entity.Property(s => s.ChangeAmount).HasColumnType("numeric(14,3)");
            entity.Property(s => s.RefundedAmount).HasColumnType("numeric(14,3)");
            entity.HasIndex(s => s.Number).IsUnique();
            entity.HasIndex(s => s.CreatedAt);
            entity.HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.Property(i => i.UnitPrice).HasColumnType("numeric(14,3)");
            entity.Property(i => i.LineTotal).HasColumnType("numeric(14,3)");
            entity.HasOne(i => i.Sale)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.Property(s => s.Language).HasMaxLength(5).IsRequired();
            entity.Property(s => s.Theme).HasMaxLength(10).IsRequired();
        });
    }
}
