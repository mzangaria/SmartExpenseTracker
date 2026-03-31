using ExpenseTracker.Api.Enums;

namespace ExpenseTracker.Api.Entities;

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid CategoryId { get; set; }

    public Category? Category { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "ILS";

    public DateOnly ExpenseDate { get; set; }

    public string? Notes { get; set; }

    public string? Merchant { get; set; }

    public CategorySource CategorySource { get; set; } = CategorySource.Manual;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
