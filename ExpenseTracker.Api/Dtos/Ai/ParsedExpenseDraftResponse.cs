namespace ExpenseTracker.Api.Dtos.Ai;

public class ParsedExpenseDraftResponse
{
    public string Description { get; set; } = string.Empty;

    public decimal? Amount { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public Guid? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string? Merchant { get; set; }

    public string? Notes { get; set; }

    public bool UseAiCategory { get; set; }
}
