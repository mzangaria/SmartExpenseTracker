namespace ExpenseTracker.Api.Dtos.Budgets;

public class BudgetVarianceResponse
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public decimal BudgetAmount { get; set; }

    public decimal ActualAmount { get; set; }

    public decimal VarianceAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public decimal UsagePercent { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool ShowInWarningStrip { get; set; }
}
