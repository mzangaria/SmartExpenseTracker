using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.Budgets;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Exceptions;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Services;

public class BudgetService(AppDbContext dbContext, ICategoryService categoryService) : IBudgetService
{
    public async Task<IReadOnlyList<BudgetResponse>> GetBudgetsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Budgets
            .AsNoTracking()
            .Include(budget => budget.Category)
            .Where(budget => budget.UserId == userId)
            .OrderBy(budget => budget.Category!.Name)
            .Select(budget => new BudgetResponse
            {
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category!.Name,
                CategoryType = budget.Category.Type.ToString().ToLowerInvariant(),
                Amount = budget.Amount,
                CreatedAtUtc = budget.CreatedAtUtc,
                UpdatedAtUtc = budget.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BudgetResponse> UpsertBudgetAsync(Guid userId, Guid categoryId, decimal amount, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetOwnedCategoryAsync(userId, categoryId, cancellationToken);
        if (category is null)
        {
            throw new InvalidCategorySelectionException(nameof(categoryId));
        }

        var budget = await dbContext.Budgets
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.UserId == userId && item.CategoryId == categoryId, cancellationToken);

        if (budget is null)
        {
            budget = new Budget
            {
                UserId = userId,
                CategoryId = categoryId,
                Category = category,
                Amount = decimal.Round(amount, 2),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.Budgets.Add(budget);
        }
        else
        {
            budget.Amount = decimal.Round(amount, 2);
            budget.UpdatedAtUtc = DateTime.UtcNow;
            budget.Category = category;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new BudgetResponse
        {
            CategoryId = budget.CategoryId,
            CategoryName = category.Name,
            CategoryType = category.Type.ToString().ToLowerInvariant(),
            Amount = budget.Amount,
            CreatedAtUtc = budget.CreatedAtUtc,
            UpdatedAtUtc = budget.UpdatedAtUtc
        };
    }

    public async Task<bool> DeleteBudgetAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken)
    {
        var budget = await dbContext.Budgets
            .FirstOrDefaultAsync(item => item.UserId == userId && item.CategoryId == categoryId, cancellationToken);

        if (budget is null)
        {
            return false;
        }

        dbContext.Budgets.Remove(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<BudgetVarianceResponse>> GetBudgetVarianceAsync(Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        var budgets = await dbContext.Budgets
            .AsNoTracking()
            .Include(budget => budget.Category)
            .Where(budget => budget.UserId == userId)
            .ToListAsync(cancellationToken);

        if (budgets.Count == 0)
        {
            return [];
        }

        var spendByCategory = await dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.UserId == userId && expense.ExpenseDate >= start && expense.ExpenseDate < end)
            .GroupBy(expense => expense.CategoryId)
            .Select(group => new
            {
                CategoryId = group.Key,
                ActualAmount = group.Sum(expense => expense.Amount)
            })
            .ToDictionaryAsync(item => item.CategoryId, item => item.ActualAmount, cancellationToken);

        return budgets
            .Select(budget =>
            {
                var actualAmount = spendByCategory.GetValueOrDefault(budget.CategoryId, 0m);
                var budgetAmount = budget.Amount;
                var varianceAmount = actualAmount - budgetAmount;
                var remainingAmount = budgetAmount - actualAmount;
                var usagePercent = budgetAmount == 0 ? 0 : Math.Round(actualAmount / budgetAmount * 100, 2);
                var status = GetStatus(usagePercent);

                return new BudgetVarianceResponse
                {
                    CategoryId = budget.CategoryId,
                    CategoryName = budget.Category?.Name ?? string.Empty,
                    BudgetAmount = budgetAmount,
                    ActualAmount = actualAmount,
                    VarianceAmount = varianceAmount,
                    RemainingAmount = remainingAmount,
                    UsagePercent = usagePercent,
                    Status = status,
                    ShowInWarningStrip = status is "warning" or "reached" or "over_budget"
                };
            })
            .OrderBy(item => GetSeverity(item.Status))
            .ThenByDescending(item => Math.Max(item.VarianceAmount, 0))
            .ThenBy(item => item.CategoryName)
            .ToList();
    }

    private static string GetStatus(decimal usagePercent)
    {
        if (usagePercent > 100)
        {
            return "over_budget";
        }

        if (usagePercent == 100)
        {
            return "reached";
        }

        if (usagePercent >= 80)
        {
            return "warning";
        }

        return "normal";
    }

    private static int GetSeverity(string status) =>
        status switch
        {
            "over_budget" => 0,
            "reached" => 1,
            "warning" => 2,
            _ => 3
        };
}
