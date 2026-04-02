using ExpenseTracker.Api.Dtos.Ai;

namespace ExpenseTracker.Api.Services.Interfaces;

public interface IAiClassificationService
{
    Task<ClassifyExpenseResponse> ClassifyExpenseAsync(Guid userId, string description, CancellationToken cancellationToken);

    Task<ParseExpenseResponse> ParseExpenseAsync(Guid userId, string text, CancellationToken cancellationToken);
}
