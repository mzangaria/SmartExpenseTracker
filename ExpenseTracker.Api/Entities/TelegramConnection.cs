namespace ExpenseTracker.Api.Entities;

public class TelegramConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public long TelegramUserId { get; set; }

    public long TelegramChatId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
