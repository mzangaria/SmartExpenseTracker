using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.Budgets;
using ExpenseTracker.Api.Dtos.Imports;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Exceptions;
using ExpenseTracker.Api.Services.Interfaces;
using ExpenseTracker.Api.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

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

    public async Task<string> ExportCsvAsync(Guid userId, CancellationToken cancellationToken)
    {
        var budgets = await dbContext.Budgets
            .AsNoTracking()
            .Include(budget => budget.Category)
            .Where(budget => budget.UserId == userId)
            .OrderBy(budget => budget.Category!.Name)
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine(CsvUtility.BuildRow([
            "CategoryName",
            "Amount"
        ]));

        foreach (var budget in budgets)
        {
            builder.AppendLine(CsvUtility.BuildRow([
                budget.Category?.Name,
                budget.Amount.ToString("0.00", CultureInfo.InvariantCulture)
            ]));
        }

        return builder.ToString();
    }

    public async Task<CsvImportResult> ImportCsvAsync(Guid userId, Stream stream, CancellationToken cancellationToken)
    {
        var result = new CsvImportResult();
        List<string[]> rows;
        try
        {
            rows = await CsvUtility.ReadRowsAsync(stream, cancellationToken);
        }
        catch (FormatException exception)
        {
            result.Errors.Add(new CsvImportError
            {
                RowNumber = 1,
                Message = $"Invalid CSV format: {exception.Message}"
            });
            return result;
        }

        if (rows.Count == 0)
        {
            result.Errors.Add(new CsvImportError { RowNumber = 1, Message = "CSV file is empty." });
            return result;
        }

        var headers = rows[0];
        var headerMap = headers
            .Select((header, index) => new { Header = header.Trim(), Index = index })
            .Where(item => !string.IsNullOrWhiteSpace(item.Header))
            .ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);

        var requiredHeaders = new[] { "CategoryName", "Amount" };
        var missingHeaders = requiredHeaders.Where(header => !headerMap.ContainsKey(header)).ToList();
        if (missingHeaders.Count > 0)
        {
            result.Errors.Add(new CsvImportError
            {
                RowNumber = 1,
                Message = $"Missing required header(s): {string.Join(", ", missingHeaders)}."
            });
            return result;
        }

        var categories = await categoryService.GetAllowedCategoryEntitiesAsync(userId, cancellationToken);
        var categoriesByName = categories.ToDictionary(category => category.Name, StringComparer.OrdinalIgnoreCase);
        var existingBudgets = await dbContext.Budgets
            .Include(budget => budget.Category)
            .Where(budget => budget.UserId == userId)
            .ToDictionaryAsync(budget => budget.CategoryId, cancellationToken);

        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var rowNumber = rowIndex + 1;
            var row = rows[rowIndex];

            try
            {
                var categoryName = headerMap["CategoryName"] < row.Length ? row[headerMap["CategoryName"]].Trim() : string.Empty;
                var amountValue = headerMap["Amount"] < row.Length ? row[headerMap["Amount"]].Trim() : string.Empty;

                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    throw new FormatException("CategoryName is required.");
                }

                if (!CsvUtility.TryParseDecimal(amountValue, out var amount) || amount <= 0)
                {
                    throw new FormatException("Amount must be a positive decimal number.");
                }

                if (!categoriesByName.TryGetValue(categoryName, out var category))
                {
                    throw new FormatException($"Unknown category '{categoryName}'.");
                }

                if (!existingBudgets.TryGetValue(category.Id, out var budget))
                {
                    budget = new Budget
                    {
                        UserId = userId,
                        CategoryId = category.Id,
                        Category = category,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    dbContext.Budgets.Add(budget);
                    existingBudgets[category.Id] = budget;
                }

                budget.Amount = decimal.Round(amount, 2);
                budget.UpdatedAtUtc = DateTime.UtcNow;
                result.ImportedCount++;
            }
            catch (FormatException exception)
            {
                result.Errors.Add(new CsvImportError { RowNumber = rowNumber, Message = exception.Message });
            }
        }

        if (result.ImportedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
