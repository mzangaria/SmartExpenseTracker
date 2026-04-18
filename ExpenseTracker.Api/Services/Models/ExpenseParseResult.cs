namespace ExpenseTracker.Api.Services.Models;

public class ExpenseParseResult
{
    public ParsedExpenseCandidate Candidate { get; set; } = new();

    public bool ShouldSave { get; set; }

    public string? ClarificationQuestion { get; set; }
}
