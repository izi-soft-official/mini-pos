using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public ActionResult<PagedResponse<ProductResponse>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;

        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query=  query.Where(p => EF.Functions.ILike(p.Sku, pattern)
                                    || EF.Functions.ILike(p.Name, pattern));
        }

        if (categoryId is not null)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        var total = query.Count();

        var items = query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                CategoryId = p.CategoryId,
                CategoryName = p.Category!.Name,
                Price = p.Price,
                Cost = p.Cost,
                Stock = p.Stock,
                IsActive = p.IsActive
            })
            .ToList();

        return new PagedResponse<ProductResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    [HttpGet("{id}")]
    public ActionResult<ProductResponse> GetProduct(int id)
    {
        var product = _db.Products
            .Include(p => p.Category)
            .FirstOrDefault(p => p.Id == id);

        if (product is null)
            return NotFound(new { error = "Product not found." });

        return ToResponse(product);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public ActionResult<ProductResponse> CreateProduct(CreateProductRequest request)
    {
        var error = Validate(request, null);
        if (error is not null)
            return BadRequest(new { error });

        var product = new Product
        {
            Sku = request.Sku.Trim(),
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            Price = request.Price,
            Cost = request.Cost,
            Stock = request.Stock,
            IsActive = request.IsActive
        };

        _db.Products.Add(product);
        _db.SaveChanges();

        product.Category = _db.Categories.First(c => c.Id == product.CategoryId);

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ToResponse(product));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public ActionResult<ProductResponse> UpdateProduct(int id, UpdateProductRequest request)
    {
        var product = _db.Products.FirstOrDefault(p => p.Id == id);
        if (product is null)
            return NotFound(new { error = "Product not found." });

        var error = Validate(request, id);
        if (error is not null)
            return BadRequest(new { error });

        product.Sku = request.Sku.Trim();
        product.Name = request.Name.Trim();
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.Cost = request.Cost;
        product.Stock = request.Stock;
        product.IsActive = request.IsActive;

        _db.SaveChanges();

        product.Category = _db.Categories.First(c => c.Id == product.CategoryId);

        return ToResponse(product);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult DeleteProduct(int id)
    {
        var product = _db.Products.FirstOrDefault(p => p.Id == id);
        if (product is null)
            return NotFound(new { error = "Product not found." });

        if (_db.SaleItems.Any(i => i.ProductId == id))
            return BadRequest(new { error = "This product appears on a sale. Deactivate it instead of deleting it." });

        _db.Products.Remove(product);
        _db.SaveChanges();

        return NoContent();
    }

    private string? Validate(CreateProductRequest request, int? currentId)
    {
        var sku = request.Sku?.Trim() ?? string.Empty;
        if (sku.Length == 0)
            return "SKU is required.";

        if (string.IsNullOrWhiteSpace(request.Name))
            return "Product name is required.";

        if (_db.Products.Any(p => p.Id != currentId && p.Sku.ToLower() == sku.ToLower()))
            return "A product with this SKU already exists.";

        if (!_db.Categories.Any(c => c.Id == request.CategoryId))
            return "Category not found.";

        if (request.Price < 0 || request.Cost < 0 || request.Stock < 0)
            return "Price, cost and stock cannot be negative.";

        return null;
    }

    private static ProductResponse ToResponse(Product product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        Name = product.Name,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? string.Empty,
        Price = product.Price,
        Cost = product.Cost,
        Stock = product.Stock,
        IsActive = product.IsActive
    };
}
