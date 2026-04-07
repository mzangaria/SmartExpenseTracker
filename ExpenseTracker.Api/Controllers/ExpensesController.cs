using ExpenseTracker.Api.Dtos.Expenses;
using ExpenseTracker.Api.Exceptions;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("expenses")]
// Controllers stay thin here: they handle HTTP concerns and delegate business rules to services.
/*
    * This file defines an ASP.NET Core API controller for expenses.
    * Its job is to receive HTTP requests, extract request data,
    * enforce request-level concerns like authorization and routing,
    * and then delegate the real business work to services.
    */
public class ExpensesController(ICurrentUserService currentUserService, IExpenseService expenseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseResponse>>> GetAll([FromQuery] ExpenseQueryParameters query, CancellationToken cancellationToken)
    {
                                                                    /*
                                                                    Why use [FromQuery]:
                                                                        It makes the source explicit and is common for filtering,
                                                                        sorting, pagination, date ranges, etc.   
                                                                    */
        var userId = currentUserService.GetRequiredUserId();
        var expenses = await expenseService.GetListAsync(userId, query, cancellationToken);
        return Ok(expenses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var expense = await expenseService.GetByIdAsync(userId, id, cancellationToken);
        return expense is null ? NotFound() : Ok(expense);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseResponse>> Create(ExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        try
        {
            var expense = await expenseService.CreateAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = expense.Id }, expense);
        }
        catch (BusinessValidationException exception)
        {
            // Service-layer validation errors are translated into an HTTP validation response.
            ModelState.AddModelError(exception.Field, exception.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExpenseResponse>> Update(Guid id, ExpenseRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        try
        {
            var expense = await expenseService.UpdateAsync(userId, id, request, cancellationToken);
            return expense is null ? NotFound() : Ok(expense);
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var deleted = await expenseService.DeleteAsync(userId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
