namespace ExpenseTracker.Api.Dtos.Ai;

public class ParseExpenseResponse
{
    public bool Success { get; set; }

    public ParsedExpenseDraftResponse Draft { get; set; } = new();

    public List<string> MissingFields { get; set; } = [];

    public List<string> Warnings { get; set; } = [];

    public string? Message { get; set; }
}
