using ExpenseTracker.Api.Dtos.FinancialMessages;
using ExpenseTracker.Api.Enums;

namespace ExpenseTracker.Api.Services.Interfaces;

public interface IFinancialMessageService
{
    Task<IReadOnlyList<FinancialMessageResponse>> GetMessagesAsync(Guid userId, FinancialMessageQueryParameters query, CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken);

    Task<FinancialMessageResponse?> MarkReadAsync(Guid userId, Guid messageId, CancellationToken cancellationToken);

    Task<FinancialMessageResponse?> DismissAsync(Guid userId, Guid messageId, CancellationToken cancellationToken);

    Task<FinancialMessageResponse?> ArchiveAsync(Guid userId, Guid messageId, CancellationToken cancellationToken);

    Task CreateSystemMessageAsync(Guid userId, FinancialMessageType type, FinancialMessageSeverity severity, string title, string message, string? contextJson, int? sourceYear, int? sourceMonth, CancellationToken cancellationToken);
}
