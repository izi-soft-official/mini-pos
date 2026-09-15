using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public ActionResult<List<CategoryResponse>> GetCategories()
    {
        return _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive
            })
            .ToList();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public ActionResult<CategoryResponse> CreateCategory(CreateCategoryRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return BadRequest(new { error = "Category name is required." });

        if (_db.Categories.Any(c => c.Name.ToLower() == name.ToLower()))
            return BadRequest(new { error = "A category with this name already exists." });

        var category = new Category { Name = name, IsActive = true };
        _db.Categories.Add(category);
        _db.SaveChanges();

        return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, ToResponse(category));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public ActionResult<CategoryResponse> UpdateCategory(int id, UpdateCategoryRequest request)
    {
        var category = _db.Categories.FirstOrDefault(c => c.Id == id);
        if (category is null)
            return NotFound(new { error = "Category not found." });

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return BadRequest(new { error = "Category name is required." });

        if (_db.Categories.Any(c => c.Id != id && c.Name.ToLower() == name.ToLower()))
            return BadRequest(new { error = "A category with this name already exists." });

        category.Name = name;
        category.IsActive = request.IsActive;
        _db.SaveChanges();

        return ToResponse(category);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult DeleteCategory(int id)
    {
        var category = _db.Categories.FirstOrDefault(c => c.Id == id);
        if (category is null)
            return NotFound(new { error = "Category not found." });

        if (_db.Products.Any(p => p.CategoryId == id))
            return BadRequest(new { error = "This category still has products. Move or delete them first." });

        _db.Categories.Remove(category);
        _db.SaveChanges();

        return NoContent();
    }

    private static CategoryResponse ToResponse(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        IsActive = category.IsActive
    };
}
