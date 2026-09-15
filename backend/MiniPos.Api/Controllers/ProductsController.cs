using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;
using MiniPos.Api.Models;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public class ProductsController(ILogger<ProductsController> logger, IProductRepo productRepo) : ControllerBase
{
    private readonly ILogger<ProductsController> _logger = logger;
    private readonly IProductRepo _repo = productRepo;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAllProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var products = await _repo.GetAllAsync(search, categoryId, activeOnly, page, pageSize);

        var response = products.Select(p => new ProductResponse(
            p.Id,
            p.Sku,
            p.Name,
            p.CategoryId,
            p.Category?.Name ?? string.Empty,
            p.Price,
            p.Cost,
            p.Stock,
            p.IsActive
        ));

        _logger.LogInformation("Retrieved {Count} products", response.Count());

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse>> GetProduct(Guid id)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        var response = new ProductResponse(
            product.Id,
            product.Sku,
            product.Name,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.Price,
            product.Cost,
            product.Stock,
            product.IsActive
        );

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ProductResponse>> CreateProduct(CreateProductRequest request)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = request.Sku,
            Name = request.Name,
            CategoryId = request.CategoryGuid,
            Price = request.Price,
            Cost = request.Cost,
            Stock = request.Stock,
            IsActive = true
        };

        await _repo.AddAsync(product);
        await _repo.SaveChangesAsync();

        var createdProduct = await _repo.GetByIdAsync(product.Id);
        _logger.LogInformation("Created new product with Name {ProductName}", createdProduct?.Name);

        var response = new ProductResponse(
            createdProduct!.Id,
            createdProduct.Sku,
            createdProduct.Name,
            createdProduct.CategoryId,
            createdProduct.Category?.Name ?? string.Empty,
            createdProduct.Price,
            createdProduct.Cost,
            createdProduct.Stock,
            createdProduct.IsActive
        );

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest updateDto)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product == null) return NotFound();

        product.Sku = updateDto.Sku;
        product.Name = updateDto.Name;
        product.CategoryId = updateDto.CategoryGuid;
        product.Price = updateDto.Price;
        product.Cost = updateDto.Cost;
        product.Stock = updateDto.Stock;
        product.IsActive = updateDto.IsActive;

        _repo.Update(product);
        await _repo.SaveChangesAsync();
        _logger.LogInformation("Updated product with Name {ProductName} successfully", product.Name);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product == null) return NotFound();

        await _repo.DeleteAsync(id);
        await _repo.SaveChangesAsync();
        _logger.LogInformation("Deleted product Successfully");

        return NoContent();
    }
}
