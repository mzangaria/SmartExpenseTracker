namespace ExpenseTracker.Api.Services.Models;

public class ParsedExpenseCandidate
{
    public decimal? Amount { get; set; }

    public string Currency { get; set; } = "ILS";

    public string? CategoryName { get; set; }

    public Guid? CategoryId { get; set; }

    public string? Merchant { get; set; }

    public DateOnly? Date { get; set; }

    public string? Note { get; set; }

    public decimal Confidence { get; set; }

    public string ParserType { get; set; } = "deterministic";

    public List<string> MissingFields { get; set; } = [];

    public string? ClarificationQuestion { get; set; }
}
