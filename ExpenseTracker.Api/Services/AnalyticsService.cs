using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.Analytics;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Services;

public class AnalyticsService(AppDbContext dbContext) : IAnalyticsService
{
    public async Task<MonthlySummaryResponse> GetMonthlySummaryAsync(Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        var expenses = await GetExpensesForMonth(userId, year, month).ToListAsync(cancellationToken);
        if (expenses.Count == 0)
        {
            return new MonthlySummaryResponse();
        }

        var total = expenses.Sum(item => item.Amount);
        return new MonthlySummaryResponse
        {
            TotalSpent = total,
            NumberOfExpenses = expenses.Count,
            AverageExpense = Math.Round(total / expenses.Count, 2),
            LargestExpense = expenses.Max(item => item.Amount),
            Currency = expenses.GroupBy(item => item.Currency).OrderByDescending(group => group.Count()).Select(group => group.Key).First()
        };
    }

    public async Task<IReadOnlyList<CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        var grouped = await GetExpensesForMonth(userId, year, month)
            .GroupBy(item => item.Category!.Name)
            .Select(group => new
            {
                CategoryName = group.Key,
                TotalAmount = group.Sum(item => item.Amount)
            })
            .OrderByDescending(group => group.TotalAmount)
            .ToListAsync(cancellationToken);

        var total = grouped.Sum(item => item.TotalAmount);
        return grouped.Select(item => new CategoryBreakdownResponse
        {
            CategoryName = item.CategoryName,
            TotalAmount = item.TotalAmount,
            PercentageOfTotal = total == 0 ? 0 : Math.Round(item.TotalAmount / total * 100, 2)
        }).ToList();
    }

    public async Task<IReadOnlyList<TrendPointResponse>> GetTrendsAsync(Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        var selectedMonth = new DateOnly(year, month, 1);
        var startMonth = selectedMonth.AddMonths(-5);

        return await dbContext.Expenses
            .AsNoTracking()
            .Where(item => item.UserId == userId &&
                           item.ExpenseDate >= startMonth &&
                           item.ExpenseDate < selectedMonth.AddMonths(1))
            .GroupBy(item => new { item.ExpenseDate.Year, item.ExpenseDate.Month })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Month)
            .Select(group => new TrendPointResponse
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                PeriodLabel = $"{group.Key.Year}-{group.Key.Month:00}",
                TotalAmount = group.Sum(item => item.Amount)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InsightResponse>> GetInsightsAsync(Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        var currentExpenses = await GetExpensesForMonth(userId, year, month).ToListAsync(cancellationToken);
        if (currentExpenses.Count == 0)
        {
            return
            [
                new InsightResponse
                {
                    Title = "Not enough data yet",
                    Message = "Not enough data yet to generate insights."
                }
            ];
        }

        var insights = new List<InsightResponse>();
        var currentTotal = currentExpenses.Sum(item => item.Amount);

        var previousMonth = new DateOnly(year, month, 1).AddMonths(-1);
        var previousExpenses = await GetExpensesForMonth(userId, previousMonth.Year, previousMonth.Month).ToListAsync(cancellationToken);
        if (previousExpenses.Count > 0)
        {
            var previousTotal = previousExpenses.Sum(item => item.Amount);
            if (previousTotal > 0)
            {
                var delta = Math.Round((currentTotal - previousTotal) / previousTotal * 100, 2);
                insights.Add(new InsightResponse
                {
                    Title = "Month-over-month change",
                    Message = delta >= 0
                        ? $"Your spending increased by {delta:0.##}% compared to last month."
                        : $"Your spending decreased by {Math.Abs(delta):0.##}% compared to last month.",
                    Context = "Compared to last month"
                });
            }
        }

        var dominantCategory = currentExpenses
            .GroupBy(item => item.Category!.Name)
            .Select(group => new { Name = group.Key, Total = group.Sum(item => item.Amount) })
            .OrderByDescending(group => group.Total)
            .First();

        var dominantShare = currentTotal == 0 ? 0 : Math.Round(dominantCategory.Total / currentTotal * 100, 2);
        if (dominantShare >= 30)
        {
            insights.Add(new InsightResponse
            {
                Title = "Top category",
                Message = $"{dominantCategory.Name} accounts for {dominantShare:0.##}% of your total spending this month."
            });
        }

        var largestExpense = currentExpenses.OrderByDescending(item => item.Amount).First();
        insights.Add(new InsightResponse
        {
            Title = "Largest expense",
            Message = $"Your largest single expense this month was {largestExpense.Category?.Name} at {largestExpense.Amount:0.00} {largestExpense.Currency}."
        });

        var monthlyHistory = await dbContext.Expenses
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .GroupBy(item => new { item.ExpenseDate.Year, item.ExpenseDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Total = group.Sum(item => item.Amount)
            })
            .OrderByDescending(item => item.Total)
            .Take(12)
            .ToListAsync(cancellationToken);

        var highestMonth = monthlyHistory.FirstOrDefault();
        if (highestMonth is not null && highestMonth.Year == year && highestMonth.Month == month && monthlyHistory.Count >= 3)
        {
            insights.Add(new InsightResponse
            {
                Title = "High spending month",
                Message = "This is your highest monthly spending in the last 12 months."
            });
        }

        return insights;
    }

    private IQueryable<Entities.Expense> GetExpensesForMonth(Guid userId, int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        return dbContext.Expenses
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(item => item.UserId == userId && item.ExpenseDate >= start && item.ExpenseDate < end);
    }
}
