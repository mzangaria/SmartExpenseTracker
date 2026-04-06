using ExpenseTracker.Api.Enums;

namespace ExpenseTracker.Api.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public CategoryType Type { get; set; } // This is an enum, (System/Custom,..if any)

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}
