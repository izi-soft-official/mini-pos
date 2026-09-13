using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;

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
        throw new NotImplementedException();
    }

    [HttpGet("{id}")]
    public ActionResult<CustomerResponse> GetCustomer(int id)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    public ActionResult<CustomerResponse> CreateCustomer(CreateCustomerRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpPut("{id}")]
    public ActionResult<CustomerResponse> UpdateCustomer(int id, UpdateCustomerRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult DeleteCustomer(int id)
    {
        throw new NotImplementedException();
    }
}
