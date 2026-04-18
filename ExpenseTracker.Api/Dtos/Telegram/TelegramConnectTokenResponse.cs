namespace ExpenseTracker.Api.Dtos.Telegram;

public class TelegramConnectTokenResponse
{
    public string DeepLink { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
