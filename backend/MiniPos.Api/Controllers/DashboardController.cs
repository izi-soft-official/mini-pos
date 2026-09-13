using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public ActionResult<DashboardSummaryResponse> GetSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        throw new NotImplementedException();
    }

    [HttpGet("top-products")]
    public ActionResult<List<TopProductResponse>> GetTopProducts(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int limit = 5)
    {
        throw new NotImplementedException();
    }

    [HttpGet("sales-by-day")]
    public ActionResult<List<SalesByDayResponse>> GetSalesByDay(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        throw new NotImplementedException();
    }
}
