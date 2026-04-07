namespace ExpenseTracker.Api.Dtos.FinancialMessages;

public class FinancialMessageResponse
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? ContextJson { get; set; }

    public int? SourceYear { get; set; }

    public int? SourceMonth { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public DateTime? DismissedAtUtc { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }
}
