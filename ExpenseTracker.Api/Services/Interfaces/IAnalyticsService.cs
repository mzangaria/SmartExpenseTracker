using ExpenseTracker.Api.Dtos.Analytics;
using ExpenseTracker.Api.Dtos.Budgets;

namespace ExpenseTracker.Api.Services.Interfaces;

public interface IAnalyticsService
{
    Task<MonthlySummaryResponse> GetMonthlySummaryAsync(Guid userId, int year, int month, CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, int year, int month, CancellationToken cancellationToken);

    Task<IReadOnlyList<TrendPointResponse>> GetTrendsAsync(Guid userId, int year, int month, CancellationToken cancellationToken);

    Task<IReadOnlyList<InsightResponse>> GetInsightsAsync(Guid userId, int year, int month, CancellationToken cancellationToken);

    Task<IReadOnlyList<BudgetVarianceResponse>> GetBudgetVarianceAsync(Guid userId, int year, int month, CancellationToken cancellationToken);
}
