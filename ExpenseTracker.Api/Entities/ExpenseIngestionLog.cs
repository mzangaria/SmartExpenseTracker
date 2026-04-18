namespace ExpenseTracker.Api.Entities;

public class ExpenseIngestionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public string Channel { get; set; } = "telegram";

    public string OriginalText { get; set; } = string.Empty;

    public string ParserType { get; set; } = string.Empty;

    public string? ParsedPayloadJson { get; set; }

    public decimal? Confidence { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? ClarificationQuestion { get; set; }

    public Guid? CreatedExpenseId { get; set; }

    public long? TelegramUpdateId { get; set; }

    public long? TelegramMessageId { get; set; }

    public long? TelegramChatId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
