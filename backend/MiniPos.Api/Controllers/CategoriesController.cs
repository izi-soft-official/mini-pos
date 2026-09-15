using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Dtos;
using MiniPos.Api.Interfaces;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public class CategoriesController(ICategoryRepo repo, ILogger<CategoriesController> logger) : ControllerBase
{
    private readonly ICategoryRepo _repo = repo;
    private readonly ILogger<CategoriesController> _logger = logger;

    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategories()
    {
        var categories = await _repo.GetAllAsync();
        var resp = categories.Select(c => new CategoryResponse(c.Id, c.Name, c.IsActive)).ToList();
        return Ok(resp);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { Message = "Name is required" });

        var existing = (await _repo.GetAllAsync()).Any(c => c.Name == request.Name);
        if (existing)
        {
            return Conflict(new { Message = "Category with the same name already exists." });
        }

        var category = new MiniPos.Api.Models.Category { Id = Guid.NewGuid(), Name = request.Name, IsActive = true };
        await _repo.AddAsync(category);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Created category {Name}", category.Name);

        var response = new CategoryResponse(category.Id, category.Name, category.IsActive);
        return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> UpdateCategory(Guid id, UpdateCategoryRequest request)
    {
        var category = await _repo.GetByIdAsync(id);
        if (category == null) return NotFound(new { message = "Category not found." });

        category.Name = request.Name;
        category.IsActive = request.IsActive;
        _repo.Update(category);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Updated category {Id}", id);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var cat = await _repo.GetByIdAsync(id);
        if (cat == null) return NotFound(new { message = "Category not found." });

        await _repo.DeleteAsync(id);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Deleted category {Id}", id);
        return NoContent();
    }
}
