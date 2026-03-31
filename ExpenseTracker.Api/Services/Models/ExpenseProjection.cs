using ExpenseTracker.Api.Dtos.Expenses;
using ExpenseTracker.Api.Entities;

namespace ExpenseTracker.Api.Services.Models;

public static class ExpenseProjection
{
    public static ExpenseResponse ToResponse(this Expense expense)
    {
        return new ExpenseResponse
        {
            Id = expense.Id,
            Description = expense.Description,
            Amount = expense.Amount,
            Currency = expense.Currency,
            ExpenseDate = expense.ExpenseDate,
            Notes = expense.Notes,
            Merchant = expense.Merchant,
            CategoryId = expense.CategoryId,
            CategoryName = expense.Category?.Name ?? string.Empty,
            CategoryType = expense.Category?.Type.ToString().ToLowerInvariant() ?? string.Empty,
            CategorySource = expense.CategorySource.ToString().ToLowerInvariant(),
            CreatedAtUtc = expense.CreatedAtUtc,
            UpdatedAtUtc = expense.UpdatedAtUtc
        };
    }
}
