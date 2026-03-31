namespace ExpenseTracker.Api.Dtos.Analytics;

public class MonthlySummaryResponse
{
    public decimal TotalSpent { get; set; }

    public int NumberOfExpenses { get; set; }

    public decimal AverageExpense { get; set; }

    public decimal LargestExpense { get; set; }

    public string Currency { get; set; } = "ILS";
}
