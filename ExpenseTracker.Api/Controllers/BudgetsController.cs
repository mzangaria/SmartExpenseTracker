using ExpenseTracker.Api.Dtos.Budgets;
using ExpenseTracker.Api.Exceptions;
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
        try
        {
            var budget = await budgetService.UpsertBudgetAsync(userId, categoryId, request.Amount, cancellationToken);
            return Ok(budget);
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpDelete("{categoryId:guid}")]
    public async Task<IActionResult> DeleteBudget(Guid categoryId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var deleted = await budgetService.DeleteBudgetAsync(userId, categoryId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
