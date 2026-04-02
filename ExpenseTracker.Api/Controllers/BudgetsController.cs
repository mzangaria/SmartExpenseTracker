using ExpenseTracker.Api.Dtos.Budgets;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("budgets")]
public class BudgetsController(ICurrentUserService currentUserService, IBudgetService budgetService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BudgetResponse>>> GetBudgets(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var budgets = await budgetService.GetBudgetsAsync(userId, cancellationToken);
        return Ok(budgets);
    }

    [HttpPut("{categoryId:guid}")]
    public async Task<ActionResult<BudgetResponse>> UpsertBudget(Guid categoryId, BudgetRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var budget = await budgetService.UpsertBudgetAsync(userId, categoryId, request.Amount, cancellationToken);
        if (budget is null)
        {
            ModelState.AddModelError(nameof(categoryId), "Budget can only be set for a valid category.");
            return ValidationProblem(ModelState);
        }

        return Ok(budget);
    }

    [HttpDelete("{categoryId:guid}")]
    public async Task<IActionResult> DeleteBudget(Guid categoryId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var deleted = await budgetService.DeleteBudgetAsync(userId, categoryId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
