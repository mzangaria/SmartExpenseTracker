using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.FinancialMessages;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Enums;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Services;

public class FinancialMessageService(AppDbContext dbContext) : IFinancialMessageService
{
    public async Task<IReadOnlyList<FinancialMessageResponse>> GetMessagesAsync(Guid userId, FinancialMessageQueryParameters query, CancellationToken cancellationToken)
    {
        var messages = dbContext.FinancialMessages
            .AsNoTracking()
            .Where(item => item.UserId == userId);

        if (TryParseStatus(query.Status, out var status))
        {
            messages = messages.Where(item => item.Status == status);
        }

        if (TryParseType(query.Type, out var type))
        {
            messages = messages.Where(item => item.Type == type);
        }

        return await messages
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => item.ToResponse())
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.FinancialMessages
            .AsNoTracking()
            .CountAsync(item => item.UserId == userId && item.Status == FinancialMessageStatus.Unread, cancellationToken);
    }

    public Task<FinancialMessageResponse?> MarkReadAsync(Guid userId, Guid messageId, CancellationToken cancellationToken)
    {
        return UpdateStatusAsync(userId, messageId, FinancialMessageStatus.Read, cancellationToken);
    }

    public Task<FinancialMessageResponse?> DismissAsync(Guid userId, Guid messageId, CancellationToken cancellationToken)
    {
        return UpdateStatusAsync(userId, messageId, FinancialMessageStatus.Dismissed, cancellationToken);
    }

    public Task<FinancialMessageResponse?> ArchiveAsync(Guid userId, Guid messageId, CancellationToken cancellationToken)
    {
        return UpdateStatusAsync(userId, messageId, FinancialMessageStatus.Archived, cancellationToken);
    }

    public async Task CreateSystemMessageAsync(Guid userId, FinancialMessageType type, FinancialMessageSeverity severity, string title, string message, string? contextJson, int? sourceYear, int? sourceMonth, CancellationToken cancellationToken)
    {
        var record = new FinancialMessage
        {
            UserId = userId,
            Type = type,
            Severity = severity,
            Title = title,
            Message = message,
            ContextJson = contextJson,
            SourceYear = sourceYear,
            SourceMonth = sourceMonth,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.FinancialMessages.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<FinancialMessageResponse?> UpdateStatusAsync(Guid userId, Guid messageId, FinancialMessageStatus status, CancellationToken cancellationToken)
    {
        var message = await dbContext.FinancialMessages
            .FirstOrDefaultAsync(item => item.Id == messageId && item.UserId == userId, cancellationToken);

        if (message is null)
        {
            return null;
        }

        message.Status = status;
        message.UpdatedAtUtc = DateTime.UtcNow;

        if (status == FinancialMessageStatus.Read)
        {
            message.ReadAtUtc ??= DateTime.UtcNow;
        }
        else if (status == FinancialMessageStatus.Dismissed)
        {
            message.DismissedAtUtc ??= DateTime.UtcNow;
        }
        else if (status == FinancialMessageStatus.Archived)
        {
            message.ArchivedAtUtc ??= DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return message.ToResponse();
    }

    private static bool TryParseStatus(string? value, out FinancialMessageStatus status)
    {
        return Enum.TryParse<FinancialMessageStatus>(value, ignoreCase: true, out status);
    }

    private static bool TryParseType(string? value, out FinancialMessageType type)
    {
        return Enum.TryParse<FinancialMessageType>(value, ignoreCase: true, out type);
    }
}

internal static class FinancialMessageMappings
{
    public static FinancialMessageResponse ToResponse(this FinancialMessage message)
    {
        return new FinancialMessageResponse
        {
            Id = message.Id,
            Type = message.Type.ToString().ToLowerInvariant(),
            Status = message.Status.ToString().ToLowerInvariant(),
            Severity = message.Severity.ToString().ToLowerInvariant(),
            Title = message.Title,
            Message = message.Message,
            ContextJson = message.ContextJson,
            SourceYear = message.SourceYear,
            SourceMonth = message.SourceMonth,
            CreatedAtUtc = message.CreatedAtUtc,
            UpdatedAtUtc = message.UpdatedAtUtc,
            ReadAtUtc = message.ReadAtUtc,
            DismissedAtUtc = message.DismissedAtUtc,
            ArchivedAtUtc = message.ArchivedAtUtc
        };
    }
}
