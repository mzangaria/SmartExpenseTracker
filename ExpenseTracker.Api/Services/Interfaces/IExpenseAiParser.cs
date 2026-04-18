using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Services.Models;

namespace ExpenseTracker.Api.Services.Interfaces;

public interface IExpenseAiParser
{
    Task<ParsedExpenseCandidate?> ParseAsync(string text, IReadOnlyList<Category> allowedCategories, CancellationToken cancellationToken);
}
