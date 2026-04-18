using ExpenseTracker.Api.Services.Models;

namespace ExpenseTracker.Api.Services.Interfaces;

public interface ITelegramBotClient
{
    Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long offset, int timeoutSeconds, CancellationToken cancellationToken);

    Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken);
}
