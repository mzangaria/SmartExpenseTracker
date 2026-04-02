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
    [HttpPost("classify-expense")]
    public async Task<ActionResult<ClassifyExpenseResponse>> ClassifyExpense(ClassifyExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var response = await aiClassificationService.ClassifyExpenseAsync(userId, request.Description, cancellationToken);
        return Ok(response);
    }

    [HttpPost("parse-expense")]
    public async Task<ActionResult<ParseExpenseResponse>> ParseExpense(ParseExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var response = await aiClassificationService.ParseExpenseAsync(userId, request.Text, cancellationToken);
        return Ok(response);
    }
}
