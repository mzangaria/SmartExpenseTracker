namespace ExpenseTracker.Api.Dtos.Analytics;

public class InsightResponse
{
    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Context { get; set; }
}
