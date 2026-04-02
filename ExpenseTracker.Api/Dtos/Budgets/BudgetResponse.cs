namespace ExpenseTracker.Api.Dtos.Budgets;

public class BudgetResponse
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string CategoryType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
