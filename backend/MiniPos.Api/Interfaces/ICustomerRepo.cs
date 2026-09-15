using MiniPos.Api.Dtos;
using MiniPos.Api.Models;

namespace MiniPos.Api.Interfaces;

public interface ICustomerRepo
{
    Task<(IEnumerable<CustomerResponse> Items, int Total)> GetPagedCustomersAsync(string? search, int page, int pageSize);
    Task<CustomerResponse?> GetByIdAsync(Guid id);
    Task<CustomerResponse> CreateCustomer(CreateCustomerRequest request);
    Task<bool> UpdateCustomer(Guid id, UpdateCustomerRequest request);
    Task<bool> DeleteCustomer(Guid id);
}
