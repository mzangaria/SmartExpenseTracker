using ExpenseTracker.Api.Enums;

namespace ExpenseTracker.Api.Entities;

public class FinancialMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public FinancialMessageType Type { get; set; } = FinancialMessageType.SystemInsight;

    public FinancialMessageStatus Status { get; set; } = FinancialMessageStatus.Unread;

    public FinancialMessageSeverity Severity { get; set; } = FinancialMessageSeverity.Low;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? ContextJson { get; set; }

    public int? SourceYear { get; set; }

    public int? SourceMonth { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAtUtc { get; set; }

    public DateTime? DismissedAtUtc { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }
}
