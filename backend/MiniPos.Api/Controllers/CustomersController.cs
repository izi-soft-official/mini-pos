using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public ActionResult<PagedResponse<CustomerResponse>> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;

        var query = _db.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.FullName, pattern)
                                  || EF.Functions.ILike(c.Phone, pattern));
        }

        var total = query.Count();

        var items = query
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerResponse
            {
                Id = c.Id,
                FullName = c.FullName,
                Phone = c.Phone,
                Email = c.Email,
                Note = c.Note,
                CreatedAt = c.CreatedAt
            })
            .ToList();

        return new PagedResponse<CustomerResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    [HttpGet("{id}")]
    public ActionResult<CustomerResponse> GetCustomer(int id)
    {
        var customer = _db.Customers.FirstOrDefault(c => c.Id == id);
        if (customer is null)
            return NotFound(new { error = "Customer not found." });

        return ToResponse(customer);
    }

    [HttpPost]
    public ActionResult<CustomerResponse> CreateCustomer(CreateCustomerRequest request)
    {
        var fullName = request.FullName?.Trim() ?? string.Empty;
        if (fullName.Length == 0)
            return BadRequest(new { error = "Customer name is required." });

        var customer = new Customer
        {
            FullName = fullName,
            Phone = request.Phone?.Trim() ?? string.Empty,
            Email = request.Email?.Trim() ?? string.Empty,
            Note = request.Note?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        _db.SaveChanges();

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, ToResponse(customer));
    }

    [HttpPut("{id}")]
    public ActionResult<CustomerResponse> UpdateCustomer(int id, UpdateCustomerRequest request)
    {
        var customer = _db.Customers.FirstOrDefault(c => c.Id == id);
        if (customer is null)
            return NotFound(new { error = "Customer not found." });

        var fullName = request.FullName?.Trim() ?? string.Empty;
        if (fullName.Length == 0)
            return BadRequest(new { error = "Customer name is required." });

        customer.FullName = fullName;
        customer.Phone = request.Phone?.Trim() ?? string.Empty;
        customer.Email = request.Email?.Trim() ?? string.Empty;
        customer.Note = request.Note?.Trim() ?? string.Empty;

        _db.SaveChanges();

        return ToResponse(customer);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult DeleteCustomer(int id)
    {
        var customer = _db.Customers.FirstOrDefault(c => c.Id == id);
        if (customer is null)
            return NotFound(new { error = "Customer not found." });

        if (_db.Sales.Any(s => s.CustomerId == id))
            return BadRequest(new { error = "This customer has sales and cannot be deleted." });

        _db.Customers.Remove(customer);
        _db.SaveChanges();

        return NoContent();
    }

    private static CustomerResponse ToResponse(Customer customer) => new()
    {
        Id = customer.Id,
        FullName = customer.FullName,
        Phone = customer.Phone,
        Email = customer.Email,
        Note = customer.Note,
        CreatedAt = customer.CreatedAt
    };
}
