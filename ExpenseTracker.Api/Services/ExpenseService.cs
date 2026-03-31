using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.Expenses;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Enums;
using ExpenseTracker.Api.Services.Interfaces;
using ExpenseTracker.Api.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Services;

public class ExpenseService(AppDbContext dbContext, ICategoryService categoryService) : IExpenseService
{
    public async Task<ExpenseResponse> CreateAsync(Guid userId, ExpenseRequest request, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetOwnedCategoryAsync(userId, request.CategoryId, cancellationToken)
            ?? throw new InvalidOperationException("Invalid category selected.");

        var expense = new Expense
        {
            UserId = userId,
            Description = request.Description.Trim(),
            Amount = decimal.Round(request.Amount, 2),
            Currency = request.Currency.Trim().ToUpperInvariant(),
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
        await dbContext.Entry(expense).Reference(item => item.Category).LoadAsync(cancellationToken);

        return expense.ToResponse();
    }

    public async Task<IReadOnlyList<ExpenseResponse>> GetListAsync(Guid userId, ExpenseQueryParameters query, CancellationToken cancellationToken)
    {
        var expenses = BuildQuery(userId, query);
        var results = await expenses
            .OrderByDescending(expense => expense.ExpenseDate)
            .ThenByDescending(expense => expense.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return results.Select(expense => expense.ToResponse()).ToList();
    }

    public async Task<ExpenseResponse?> GetByIdAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken)
    {
        var expense = await dbContext.Expenses
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.Id == expenseId && item.UserId == userId, cancellationToken);

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
            ?? throw new InvalidOperationException("Invalid category selected.");

        expense.Description = request.Description.Trim();
        expense.Amount = decimal.Round(request.Amount, 2);
        expense.Currency = request.Currency.Trim().ToUpperInvariant();
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
            var search = query.Search.Trim().ToLowerInvariant();
            expenses = expenses.Where(item => item.Description.ToLower().Contains(search));
        }

        return expenses;
    }
}
