namespace ExpenseTracker.Api.Dtos.Analytics;

public class CategoryBreakdownResponse
{
    public string CategoryName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal PercentageOfTotal { get; set; }
}
