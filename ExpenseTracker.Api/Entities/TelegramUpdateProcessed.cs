namespace ExpenseTracker.Api.Entities;

public class TelegramUpdateProcessed
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public long UpdateId { get; set; }

    public long? MessageId { get; set; }

    public long? ChatId { get; set; }

    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
