using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/dashboard")]
public class DashboardController(IDashboardRepo repo, ILogger<DashboardController> logger) : ControllerBase
{
    private readonly IDashboardRepo _repo = repo;
    private readonly ILogger<DashboardController> _logger = logger;

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var fromDt = from.HasValue ? from.Value.ToDateTime(new TimeOnly(0)) : DateTime.MinValue;
        var toDt = to.HasValue ? to.Value.ToDateTime(new TimeOnly(23, 59, 59)) : DateTime.MaxValue;

        var resp = await _repo.GetSummaryAsync(fromDt, toDt);
        return Ok(resp);
    }

    [HttpGet("top-products")]
    public async Task<ActionResult<List<TopProductResponse>>> GetTopProducts(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int limit = 5)
    {
        var fromDt = from.HasValue ? from.Value.ToDateTime(new TimeOnly(0)) : DateTime.MinValue;
        var toDt = to.HasValue ? to.Value.ToDateTime(new TimeOnly(23, 59, 59)) : DateTime.MaxValue;

        var list = await _repo.GetTopProductsAsync(fromDt, toDt, limit);
        return Ok(list);
    }

    [HttpGet("sales-by-day")]
    public async Task<ActionResult<List<SalesByDayResponse>>> GetSalesByDay(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var fromDt = from.HasValue ? from.Value.ToDateTime(new TimeOnly(0)) : DateTime.MinValue;
        var toDt = to.HasValue ? to.Value.ToDateTime(new TimeOnly(23, 59, 59)) : DateTime.MaxValue;

        var list = await _repo.GetSalesByDayAsync(fromDt, toDt);
        return Ok(list);
    }
}
