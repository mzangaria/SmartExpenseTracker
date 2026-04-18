namespace ExpenseTracker.Api.Configuration;

public class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = string.Empty;

    public string BotUsername { get; set; } = string.Empty;

    public bool EnablePolling { get; set; }

    public int PollingIntervalSeconds { get; set; } = 2;

    public int LinkTokenMinutes { get; set; } = 10;
}
