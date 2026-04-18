using ExpenseTracker.Api.Services.Models;

namespace ExpenseTracker.Api.Services.Interfaces;

public interface ITelegramUpdateHandler
{
    Task HandleAsync(TelegramUpdate update, CancellationToken cancellationToken);
}
