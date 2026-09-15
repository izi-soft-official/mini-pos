using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController(ICustomerRepo repo, ILogger<CustomersController> logger) : ControllerBase
{
    private readonly ICustomerRepo _repo = repo;
    private readonly ILogger<CustomersController> _logger = logger;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerResponse>>> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, total) = await _repo.GetPagedCustomersAsync(search, page, pageSize);
        var resp = new PagedResponse<CustomerResponse> { Items = items.ToList(), Page = page, PageSize = pageSize, Total = total };
        return Ok(resp);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(Guid id)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer == null) return NotFound(new { message = "Customer not found." });
        return Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(CreateCustomerRequest request)
    {
        var created = await _repo.CreateCustomer(request);
        _logger.LogInformation("Created customer {Name}", created.FullName);
        return CreatedAtAction(nameof(GetCustomer), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateCustomer(Guid id, UpdateCustomerRequest request)
    {
        var ok = await _repo.UpdateCustomer(id, request);
        if (!ok) return NotFound(new { message = "Customer not found." });
        _logger.LogInformation("Updated customer {Id}", id);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        var ok = await _repo.DeleteCustomer(id);
        if (!ok) return NotFound(new { message = "Customer not found." });
        _logger.LogInformation("Deleted customer {Id}", id);
        return NoContent();
    }
}
