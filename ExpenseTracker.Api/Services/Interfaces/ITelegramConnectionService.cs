using ExpenseTracker.Api.Dtos.Telegram;
using ExpenseTracker.Api.Services.Models;

namespace ExpenseTracker.Api.Services.Interfaces;

public interface ITelegramConnectionService
{
    Task<TelegramStatusResponse> GetStatusAsync(Guid userId, CancellationToken cancellationToken);

    Task<TelegramConnectTokenResponse> CreateConnectTokenAsync(Guid userId, CancellationToken cancellationToken);

    Task DisconnectAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> LinkAsync(string token, TelegramMessage message, CancellationToken cancellationToken);
}
