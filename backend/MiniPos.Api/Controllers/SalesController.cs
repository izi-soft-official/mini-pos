using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SalesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public ActionResult<PagedResponse<SaleListItemResponse>> GetSales(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? customerId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id}")]
    public ActionResult<SaleResponse> GetSale(int id)
    {
        throw new NotImplementedException();
    }

    // Read the stock rows for update and decrement them in the same transaction as the insert,
    // otherwise two cashiers can sell the last unit at the same time.
    [HttpPost]
    public ActionResult<SaleResponse> CreateSale(CreateSaleRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{id}/return")]
    [Authorize(Roles = "Admin,Manager")]
    public ActionResult<SaleResponse> ReturnSale(int id, CreateReturnRequest request)
    {
        throw new NotImplementedException();
    }
}
