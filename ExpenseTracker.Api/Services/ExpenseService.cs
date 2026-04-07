using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.Expenses;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Enums;
using ExpenseTracker.Api.Exceptions;
using ExpenseTracker.Api.Services.Interfaces;
using ExpenseTracker.Api.Services.Models;
using Microsoft.EntityFrameworkCore;
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
}
