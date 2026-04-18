namespace ExpenseTracker.Api.Services.Interfaces;

using ExpenseTracker.Api.Services.Models;

public interface IExpenseMessageParser
{
    Task<ExpenseParseResult> ParseAsync(Guid userId, string text, CancellationToken cancellationToken);
}
