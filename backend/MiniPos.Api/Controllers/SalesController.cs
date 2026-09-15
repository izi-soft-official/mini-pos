using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sales")]
public class SalesController(ISaleRepo repo, ILogger<SalesController> logger) : ControllerBase
{
    private readonly ISaleRepo _repo = repo;
    private readonly ILogger<SalesController> _logger = logger;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<SaleListItemResponse>>> GetSales(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var fromDt = from.HasValue ? from.Value.ToDateTime(new TimeOnly(0)) : DateTime.MinValue;
        var toDt = to.HasValue ? to.Value.ToDateTime(new TimeOnly(23, 59, 59)) : DateTime.MaxValue;

        var (items, total) = await _repo.GetPagedSalesAsync(fromDt, toDt, customerId, status, page, pageSize);
        var resp = new PagedResponse<SaleListItemResponse> { Items = items.ToList(), Page = page, PageSize = pageSize, Total = total };
        return Ok(resp);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleResponse>> GetSale(Guid id)
    {
        var sale = await _repo.GetByIdAsync(id);
        if (sale == null) return NotFound(new { message = "Sale not found." });
        return Ok(sale);
    }

    [HttpPost]
    public async Task<ActionResult<SaleResponse>> CreateSale(CreateSaleRequest request)
    {
        if (request.Items == null || request.Items.Count == 0) return BadRequest(new { Message = "No items provided" });

        Guid? userId = null;
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var uid)) userId = uid;

        try
        {
            var created = await _repo.CreateSaleAsync(request, userId);
            _logger.LogInformation("Created sale {Number}", created.Number);
            return CreatedAtAction(nameof(GetSale), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/return")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SaleResponse>> ReturnSale(Guid id, CreateReturnRequest request)
    {
        try
        {
            var res = await _repo.ReturnSaleAsync(id, request);
            return Ok(res);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
