using ExpenseTracker.Api.Dtos.Categories;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("categories")]
public class CategoriesController(ICurrentUserService currentUserService, ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var categories = await categoryService.GetAvailableCategoriesAsync(userId, cancellationToken);
        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var category = await categoryService.CreateCustomCategoryAsync(userId, request.Name, cancellationToken);
        if (category is null)
        {
            ModelState.AddModelError(nameof(request.Name), "Category name already exists or is invalid.");
            return ValidationProblem(ModelState);
        }

        return CreatedAtAction(nameof(GetAll), category);
    }
}
