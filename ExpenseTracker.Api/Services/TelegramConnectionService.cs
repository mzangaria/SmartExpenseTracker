using System.Security.Cryptography;
using System.Text;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.Telegram;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Services.Interfaces;
using ExpenseTracker.Api.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Api.Services;

public class TelegramConnectionService(
    AppDbContext dbContext,
    IOptions<TelegramOptions> options) : ITelegramConnectionService
{
    private readonly TelegramOptions _options = options.Value;

    public async Task<TelegramStatusResponse> GetStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        var connection = await dbContext.TelegramConnections
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.IsActive)
            .OrderByDescending(item => item.LinkedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new TelegramStatusResponse
        {
            IsConnected = connection is not null,
            TelegramChatId = connection?.TelegramChatId,
            TelegramUserId = connection?.TelegramUserId,
            LinkedAtUtc = connection?.LinkedAtUtc
        };
    }

    public async Task<TelegramConnectTokenResponse> CreateConnectTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotUsername))
        {
            throw new InvalidOperationException("Telegram bot username is not configured.");
        }

        var token = CreateToken();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(Math.Max(1, _options.LinkTokenMinutes));

        dbContext.TelegramLinkTokens.Add(new TelegramLinkToken
        {
            UserId = userId,
            TokenHash = HashToken(token),
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TelegramConnectTokenResponse
        {
            DeepLink = $"https://t.me/{_options.BotUsername.TrimStart('@')}?start={Uri.EscapeDataString(token)}",
            ExpiresAtUtc = expiresAt
        };
    }

    public async Task DisconnectAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var connections = await dbContext.TelegramConnections
            .Where(item => item.UserId == userId && item.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var connection in connections)
        {
            connection.IsActive = false;
            connection.RevokedAtUtc = now;
            connection.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> LinkAsync(string token, TelegramMessage message, CancellationToken cancellationToken)
    {
        if (message.From is null || message.Chat is null || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var tokenHash = HashToken(token);
        var now = DateTime.UtcNow;
        var linkToken = await dbContext.TelegramLinkTokens
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (linkToken is null || linkToken.ConsumedAtUtc is not null || linkToken.ExpiresAtUtc < now)
        {
            return false;
        }

        var activeConnections = await dbContext.TelegramConnections
            .Where(item =>
                item.IsActive &&
                (item.UserId == linkToken.UserId ||
                 item.TelegramChatId == message.Chat.Id ||
                 item.TelegramUserId == message.From.Id))
            .ToListAsync(cancellationToken);

        foreach (var connection in activeConnections)
        {
            connection.IsActive = false;
            connection.RevokedAtUtc = now;
            connection.UpdatedAtUtc = now;
        }

        dbContext.TelegramConnections.Add(new TelegramConnection
        {
            UserId = linkToken.UserId,
            TelegramUserId = message.From.Id,
            TelegramChatId = message.Chat.Id,
            IsActive = true,
            LinkedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        linkToken.ConsumedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string CreateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
