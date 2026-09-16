using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;
using MiniPos.Api.Models;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategories()
    {
        var categories = await _db.Categories.ToListAsync();
        var resp = categories.Select(c => new CategoryResponse(c.Id, c.Name, c.IsActive)).ToList();
        return Ok(resp);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "Name is required" });

        var existing = await _db.Categories.AnyAsync(c => c.Name == request.Name);
        if (existing)
        {
            return Conflict(new { error = "Category with the same name already exists." });
        }

        var category = new Category { Name = request.Name, IsActive = true };
        await _db.Categories.AddAsync(category);
        await _db.SaveChangesAsync();

        var response = new CategoryResponse(category.Id, category.Name, category.IsActive);
        return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> UpdateCategory(int id, UpdateCategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound(new { error = "Category not found." });

        category.Name = request.Name;
        category.IsActive = request.IsActive;
        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated Category successfully." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound(new { error = "Category not found." });

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted Category successfully." });
    }
}
