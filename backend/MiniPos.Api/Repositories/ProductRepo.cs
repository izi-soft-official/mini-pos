using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Interfaces;
using MiniPos.Api.Models;

namespace MiniPos.Api.Repositories
{
    public class ProductRepo(AppDbContext Db) : IProductRepo
    {
        private readonly AppDbContext _db = Db;


        public async Task<IEnumerable<Product>> GetAllAsync(string? search, Guid? categoryId, bool activeOnly, int page, int pageSize)
        {
            var query = _db.Products.Include(p => p.Category).AsQueryable();

            if (activeOnly)
            {
                query = query.Where(p => p.IsActive);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
            }

            var skip = (page - 1) * pageSize;

            return await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        }


        public async Task AddAsync(Product product)
        {
            await _db.Products.AddAsync(product);
        }

        public void Update(Product product)
        {
            _db.Products.Update(product);
        }

        public async Task DeleteAsync(Guid id)
        {
            var product = await GetByIdAsync(id);
            if (product != null)
            {
                _db.Products.Remove(product);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
