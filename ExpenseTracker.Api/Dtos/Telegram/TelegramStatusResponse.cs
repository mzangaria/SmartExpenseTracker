namespace ExpenseTracker.Api.Dtos.Telegram;

public class TelegramStatusResponse
{
    public bool IsConnected { get; set; }

    public long? TelegramChatId { get; set; }

    public long? TelegramUserId { get; set; }

    public DateTime? LinkedAtUtc { get; set; }
}
