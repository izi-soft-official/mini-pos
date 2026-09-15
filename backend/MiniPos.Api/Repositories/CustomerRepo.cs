using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;
using MiniPos.Api.Models;

namespace MiniPos.Api.Repositories;

public class CustomerRepo(AppDbContext Db) : ICustomerRepo
{
    private readonly AppDbContext _db = Db;

    public async Task<CustomerResponse> CreateCustomer(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return new CustomerResponse(customer.Id, customer.FullName, customer.Phone, customer.Email, customer.Note, customer.CreatedAt);
    }

    public async Task<bool> DeleteCustomer(Guid id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c == null) return false;
        _db.Customers.Remove(c);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(IEnumerable<CustomerResponse> Items, int Total)> GetPagedCustomersAsync(string? search, int page, int pageSize)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.FullName.Contains(search));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerResponse(c.Id, c.FullName, c.Phone, c.Email, c.Note, c.CreatedAt))
            .ToListAsync();

        return (items, total);
    }

    public async Task<CustomerResponse?> GetByIdAsync(Guid id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c == null) return null;
        return new CustomerResponse(c.Id, c.FullName, c.Phone, c.Email, c.Note, c.CreatedAt);
    }

    public async Task<bool> UpdateCustomer(Guid id, UpdateCustomerRequest request)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c == null) return false;

        c.FullName = request.FullName;
        c.Phone = request.Phone;
        c.Email = request.Email;
        c.Note = request.Note;
        await _db.SaveChangesAsync();
        return true;
    }
}
