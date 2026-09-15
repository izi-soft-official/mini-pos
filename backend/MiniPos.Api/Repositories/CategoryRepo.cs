using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Interfaces;
using MiniPos.Api.Models;

namespace MiniPos.Api.Repositories;

public class CategoryRepo(AppDbContext Db) : ICategoryRepo
{
    private readonly AppDbContext _db = Db;

    public async Task AddAsync(Category category)
    {
        await _db.Categories.AddAsync(category);
    }

    public async Task DeleteAsync(Guid id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c != null) _db.Categories.Remove(c);
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _db.Categories.AsNoTracking().ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _db.Categories.FindAsync(id);
    }

    public void Update(Category category)
    {
        _db.Categories.Update(category);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _db.SaveChangesAsync() > 0;
    }
}
