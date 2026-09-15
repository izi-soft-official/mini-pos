using MiniPos.Api.Dtos;
using MiniPos.Api.Models;

namespace MiniPos.Api.Interfaces;

public interface ICategoryRepo
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(Guid id);
    Task AddAsync(Category category);
    void Update(Category category);
    Task DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
}
