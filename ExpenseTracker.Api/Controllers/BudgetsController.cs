using ExpenseTracker.Api.Dtos.Budgets;
using ExpenseTracker.Api.Dtos.Imports;
using ExpenseTracker.Api.Exceptions;
using ExpenseTracker.Api.Services.Interfaces;
using System.Text;
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

    [HttpGet("export/csv")]
    public async Task<FileContentResult> ExportCsv(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var csv = await budgetService.ExportCsvAsync(userId, cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "budgets.csv");
    }

    [HttpPost("import/csv")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<ActionResult<CsvImportResult>> ImportCsv(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "CSV file is empty.");
            return ValidationProblem(ModelState);
        }

        var userId = currentUserService.GetRequiredUserId();
        await using var stream = file.OpenReadStream();
        var result = await budgetService.ImportCsvAsync(userId, stream, cancellationToken);
        return Ok(result);
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
