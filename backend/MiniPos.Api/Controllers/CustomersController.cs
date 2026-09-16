using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerResponse>>> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(c => EF.Functions.ILike(c.FullName, pattern) || EF.Functions.ILike(c.Email, pattern) || EF.Functions.ILike(c.Phone, pattern));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(c => c.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CustomerResponse(c.Id, c.FullName, c.Phone, c.Email, c.Note, c.CreatedAt))
            .ToListAsync();

        var resp = new PagedResponse<CustomerResponse> { Items = items, Page = page, PageSize = pageSize, Total = total };
        return Ok(resp);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { error = "Customer not found." });
        var dto = new CustomerResponse(customer.Id, customer.FullName, customer.Phone, customer.Email, customer.Note, customer.CreatedAt);
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(CreateCustomerRequest request)
    {
        var customer = new Customer { FullName = request.FullName, Phone = request.Phone, Email = request.Email, Note = request.Note, CreatedAt = DateTime.UtcNow };
        await _db.Customers.AddAsync(customer);
        await _db.SaveChangesAsync();
        var dto = new CustomerResponse(customer.Id, customer.FullName, customer.Phone, customer.Email, customer.Note, customer.CreatedAt);
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateCustomer(int id, UpdateCustomerRequest request)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { error = "Customer not found." });
        customer.FullName = request.FullName;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Note = request.Note;
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated Customer successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { error = "Customer not found." });
        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted Customer successfully." });
    }
}
