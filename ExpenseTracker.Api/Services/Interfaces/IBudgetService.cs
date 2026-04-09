using ExpenseTracker.Api.Dtos.Budgets;
using ExpenseTracker.Api.Dtos.Imports;

namespace ExpenseTracker.Api.Services.Interfaces;

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetResponse>> GetBudgetsAsync(Guid userId, CancellationToken cancellationToken);

    Task<BudgetResponse> UpsertBudgetAsync(Guid userId, Guid categoryId, decimal amount, CancellationToken cancellationToken);

    Task<bool> DeleteBudgetAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BudgetVarianceResponse>> GetBudgetVarianceAsync(Guid userId, int year, int month, CancellationToken cancellationToken);

    Task<string> ExportCsvAsync(Guid userId, CancellationToken cancellationToken);

    Task<CsvImportResult> ImportCsvAsync(Guid userId, Stream stream, CancellationToken cancellationToken);
}
