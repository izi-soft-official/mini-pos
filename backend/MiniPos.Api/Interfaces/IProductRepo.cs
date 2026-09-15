using MiniPos.Api.Dtos;
using MiniPos.Api.Models;

namespace MiniPos.Api.Interfaces
{
    public interface IProductRepo
    {
        Task<IEnumerable<Product>> GetAllAsync(string? search, Guid? categoryId, bool activeOnly, int page, int pageSize);
        Task<Product?> GetByIdAsync(Guid id);
        Task AddAsync(Product product);
        void Update(Product product);
        Task DeleteAsync(Guid id);
        Task<bool> SaveChangesAsync();
    }
}
