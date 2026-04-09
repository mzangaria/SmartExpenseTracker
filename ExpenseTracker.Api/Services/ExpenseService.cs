using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.Expenses;
using ExpenseTracker.Api.Dtos.Imports;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Enums;
using ExpenseTracker.Api.Exceptions;
using ExpenseTracker.Api.Services.Interfaces;
using ExpenseTracker.Api.Services.Models;
using ExpenseTracker.Api.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
namespace ExpenseTracker.Api.Services;

// ExpenseService owns expense business rules and EF Core query composition.
public class ExpenseService(AppDbContext dbContext, ICategoryService categoryService) : IExpenseService
    {    //the inputs called Dependency Injection (DI). 
        // The framework will automatically provide the required dependencies (AppDbContext and ICategoryService),
        // when creating an instance of ExpenseService. 
    private const string ManagedCurrency = "ILS";

    public async Task<ExpenseResponse> CreateAsync(Guid userId, ExpenseRequest request, CancellationToken cancellationToken)
    {
        // A user can only create expenses against categories they are allowed to use.
        var category = await categoryService.GetOwnedCategoryAsync(userId, request.CategoryId, cancellationToken)
            ?? throw new InvalidCategorySelectionException(nameof(request.CategoryId));

        var expense = new Expense
        {
            UserId = userId,
            Description = request.Description.Trim(),
            Amount = decimal.Round(request.Amount, 2),
            Currency = ManagedCurrency,
            CategoryId = category.Id,
            ExpenseDate = request.ExpenseDate,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Merchant = string.IsNullOrWhiteSpace(request.Merchant) ? null : request.Merchant.Trim(),
            CategorySource = request.UseAiCategory ? CategorySource.Ai : CategorySource.Manual,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Expenses.Add(expense);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(expense).Reference(item => item.Category).LoadAsync(cancellationToken); // Category is foreign key.
            // load the category navigation property after saving, so that it can be included in the response DTO.

        return expense.ToResponse();
    }

    public async Task<IReadOnlyList<ExpenseResponse>> GetListAsync(Guid userId, ExpenseQueryParameters query, CancellationToken cancellationToken)
    {
        var expenses = BuildQuery(userId, query); // Build the query with filters, but do not execute it yet.
        var results = await expenses
            .OrderByDescending(expense => expense.ExpenseDate)
            .ThenByDescending(expense => expense.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return results.Select(expense => expense.ToResponse()).ToList();
    }

    public async Task<ExpenseResponse?> GetByIdAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken)
    {
        var expense = await dbContext.Expenses
            .AsNoTracking() // read-only query, no tracking needed for updates, improves performance.
            .Include(item => item.Category) //
            .FirstOrDefaultAsync(item => item.Id == expenseId && item.UserId == userId, cancellationToken); //takes first result. if not found, returns null.

        return expense?.ToResponse();
    }

    public async Task<ExpenseResponse?> UpdateAsync(Guid userId, Guid expenseId, ExpenseRequest request, CancellationToken cancellationToken)
    {
        var expense = await dbContext.Expenses
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.Id == expenseId && item.UserId == userId, cancellationToken);

        if (expense is null)
        {
            return null;
        }

        var category = await categoryService.GetOwnedCategoryAsync(userId, request.CategoryId, cancellationToken)
            ?? throw new InvalidCategorySelectionException(nameof(request.CategoryId));

        expense.Description = request.Description.Trim();
        expense.Amount = decimal.Round(request.Amount, 2);
        expense.Currency = ManagedCurrency;
        expense.CategoryId = category.Id;
        expense.Category = category;
        expense.ExpenseDate = request.ExpenseDate;
        expense.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        expense.Merchant = string.IsNullOrWhiteSpace(request.Merchant) ? null : request.Merchant.Trim();
        expense.CategorySource = request.UseAiCategory ? CategorySource.Ai : CategorySource.Manual;
        expense.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return expense.ToResponse();
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken)
    {
        var expense = await dbContext.Expenses
            .FirstOrDefaultAsync(item => item.Id == expenseId && item.UserId == userId, cancellationToken);

        if (expense is null)
        {
            return false;
        }

        dbContext.Expenses.Remove(expense);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string> ExportCsvAsync(Guid userId, ExpenseQueryParameters query, CancellationToken cancellationToken)
    {
        var expenses = await BuildQuery(userId, query)
            .OrderByDescending(expense => expense.ExpenseDate)
            .ThenByDescending(expense => expense.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine(CsvUtility.BuildRow([
            "Description",
            "Amount",
            "Currency",
            "ExpenseDate",
            "CategoryName",
            "Merchant",
            "Notes",
            "CategorySource"
        ]));

        foreach (var expense in expenses)
        {
            builder.AppendLine(CsvUtility.BuildRow([
                expense.Description,
                expense.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                expense.Currency,
                expense.ExpenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                expense.Category?.Name,
                expense.Merchant,
                expense.Notes,
                expense.CategorySource.ToString().ToLowerInvariant()
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
        var requiredHeaders = new[] { "Description", "Amount", "Currency", "ExpenseDate", "CategoryName" };
        var headerMap = BuildHeaderMap(headers);

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

        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var rowNumber = rowIndex + 1;
            var row = rows[rowIndex];

            try
            {
                var description = GetValue(row, headerMap, "Description").Trim();
                var amountValue = GetValue(row, headerMap, "Amount").Trim();
                var currency = GetValue(row, headerMap, "Currency").Trim();
                var expenseDateValue = GetValue(row, headerMap, "ExpenseDate").Trim();
                var categoryName = GetValue(row, headerMap, "CategoryName").Trim();
                var merchant = GetOptionalValue(row, headerMap, "Merchant");
                var notes = GetOptionalValue(row, headerMap, "Notes");
                var categorySourceValue = GetOptionalValue(row, headerMap, "CategorySource");

                if (string.IsNullOrWhiteSpace(description))
                {
                    throw new FormatException("Description is required.");
                }

                if (!CsvUtility.TryParseDecimal(amountValue, out var amount) || amount <= 0)
                {
                    throw new FormatException("Amount must be a positive decimal number.");
                }

                if (!string.Equals(currency, ManagedCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException("Currency must be ILS.");
                }

                if (!CsvUtility.TryParseDateOnly(expenseDateValue, out var expenseDate))
                {
                    throw new FormatException("ExpenseDate must use yyyy-MM-dd format.");
                }

                if (!categoriesByName.TryGetValue(categoryName, out var category))
                {
                    throw new FormatException($"Unknown category '{categoryName}'.");
                }

                var categorySource = string.Equals(categorySourceValue, "ai", StringComparison.OrdinalIgnoreCase)
                    ? CategorySource.Ai
                    : CategorySource.Manual;

                dbContext.Expenses.Add(new Expense
                {
                    UserId = userId,
                    Description = description,
                    Amount = decimal.Round(amount, 2),
                    Currency = ManagedCurrency,
                    CategoryId = category.Id,
                    ExpenseDate = expenseDate,
                    Merchant = string.IsNullOrWhiteSpace(merchant) ? null : merchant.Trim(),
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                    CategorySource = categorySource,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });

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

    private IQueryable<Expense> BuildQuery(Guid userId, ExpenseQueryParameters query)
    {
        // Build the EF query first; execution happens later when materialized with ToListAsync/FirstOrDefaultAsync.
        var expenses = dbContext.Expenses
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(item => item.UserId == userId);

        if (query.Year is int year && query.Month is int month)
        {
            var start = new DateOnly(year, month, 1);
            var end = start.AddMonths(1);
            expenses = expenses.Where(item => item.ExpenseDate >= start && item.ExpenseDate < end);
        }

        if (query.CategoryId is Guid categoryId)
        {
            expenses = expenses.Where(item => item.CategoryId == categoryId);
        }

        if (query.MinAmount is decimal minAmount)
        {
            expenses = expenses.Where(item => item.Amount >= minAmount);
        }

        if (query.MaxAmount is decimal maxAmount)
        {
            expenses = expenses.Where(item => item.Amount <= maxAmount);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            expenses = ApplySearch(expenses, search);
        }

        return expenses;
    }

    private IQueryable<Expense> ApplySearch(IQueryable<Expense> expenses, string search)
    {
        var escapedSearch = EscapeLikePattern(search); 

        if (dbContext.Database.IsNpgsql()) 
        {
            // PostgreSQL gets case-insensitive search translated to SQL via ILike.
            return expenses.Where(item => EF.Functions.ILike(item.Description, $"%{escapedSearch}%", @"\"));
        }

        var normalizedSearch = search.ToUpperInvariant();
        return expenses.Where(item => item.Description.ToUpper().Contains(normalizedSearch));
    }

    private static string EscapeLikePattern(string value)
    { // Escape special characters for SQL LIKE pattern. In EF Core, 
    // we can specify the escape character (here we use backslash) in the query, so we need to escape it in the search term as well.
        return value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }

    private static Dictionary<string, int> BuildHeaderMap(string[] headers)
    {
        return headers
            .Select((header, index) => new { Header = header.Trim(), Index = index })
            .Where(item => !string.IsNullOrWhiteSpace(item.Header))
            .ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetValue(string[] row, IReadOnlyDictionary<string, int> headerMap, string header)
    {
        var index = headerMap[header];
        return index < row.Length ? row[index] : string.Empty;
    }

    private static string? GetOptionalValue(string[] row, IReadOnlyDictionary<string, int> headerMap, string header)
    {
        return headerMap.TryGetValue(header, out var index) && index < row.Length ? row[index] : null;
    }
}
