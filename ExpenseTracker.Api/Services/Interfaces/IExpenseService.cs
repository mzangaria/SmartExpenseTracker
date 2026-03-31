using ExpenseTracker.Api.Dtos.Expenses;

namespace ExpenseTracker.Api.Services.Interfaces;

public interface IExpenseService
{
    Task<ExpenseResponse> CreateAsync(Guid userId, ExpenseRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ExpenseResponse>> GetListAsync(Guid userId, ExpenseQueryParameters query, CancellationToken cancellationToken);

    Task<ExpenseResponse?> GetByIdAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken);

    Task<ExpenseResponse?> UpdateAsync(Guid userId, Guid expenseId, ExpenseRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken);
}
