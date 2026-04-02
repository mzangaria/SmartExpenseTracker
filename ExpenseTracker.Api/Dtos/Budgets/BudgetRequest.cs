using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos.Budgets;

public class BudgetRequest
{
    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Amount { get; set; }
}
