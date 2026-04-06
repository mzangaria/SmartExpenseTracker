using ExpenseTracker.Api.Dtos.Ai;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("ai")]
public class AiController(ICurrentUserService currentUserService, IAiClassificationService aiClassificationService) : ControllerBase
{
    [HttpPost("classify-expense")] // this function is for classifying an expense description into a category.
    public async Task<ActionResult<ClassifyExpenseResponse>> ClassifyExpense(ClassifyExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var response = await aiClassificationService.ClassifyExpenseAsync(userId, request.Description, cancellationToken);
        return Ok(response);
    }

    [HttpPost("parse-expense")] // this function is for parsing structured data from an unstructured text, like "I spent $20 on lunch yesterday" into an amount, category, and date.
    public async Task<ActionResult<ParseExpenseResponse>> ParseExpense(ParseExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var response = await aiClassificationService.ParseExpenseAsync(userId, request.Text, cancellationToken);
        return Ok(response);
    }
}
