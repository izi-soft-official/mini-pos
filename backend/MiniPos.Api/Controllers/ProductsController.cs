using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<ActionResult> GetAllProducts(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern) || EF.Functions.ILike(p.Sku, pattern));
        }

        var total = await query.CountAsync();
        var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        if (products.Count < 1)
        {
            return NotFound(new { error = "No products found." });
        }

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



        var list = response.ToList();
        return Ok(new { items = list, page = page, pageSize = pageSize, total });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetProduct(int id)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound(new { error = "Product not found." });
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
            // Id is an int identity column - let the database assign it
            Sku = request.Sku,
            Name = request.Name,
            CategoryId = request.CategoryId,
            Price = request.Price,
            Cost = request.Cost,
            Stock = request.Stock,
            IsActive = true
        };

        await _db.Products.AddAsync(product);
        await _db.SaveChangesAsync();

        var createdProduct = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == product.Id);


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

        return CreatedAtAction(nameof(GetProduct), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, UpdateProductRequest updateDto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound(new { error = "Product not found." });

        product.Sku = updateDto.Sku;
        product.Name = updateDto.Name;
        product.CategoryId = updateDto.CategoryId;
        product.Price = updateDto.Price;
        product.Cost = updateDto.Cost;
        product.Stock = updateDto.Stock;
        product.IsActive = updateDto.IsActive;

        _db.Products.Update(product);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Updated Product Successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound(new { error = "Product not found." });

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Deleted Product Successfully." });
    }
}
