using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;

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
        throw new NotImplementedException();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public ActionResult<CategoryResponse> CreateCategory(CreateCategoryRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public ActionResult<CategoryResponse> UpdateCategory(int id, UpdateCategoryRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult DeleteCategory(int id)
    {
        throw new NotImplementedException();
    }
}
