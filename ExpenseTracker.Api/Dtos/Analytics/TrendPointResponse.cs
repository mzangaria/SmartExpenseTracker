namespace ExpenseTracker.Api.Dtos.Analytics;

public class TrendPointResponse
{
    public string PeriodLabel { get; set; } = string.Empty;

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal TotalAmount { get; set; }
}
