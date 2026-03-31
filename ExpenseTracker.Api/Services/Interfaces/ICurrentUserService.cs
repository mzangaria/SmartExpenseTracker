namespace ExpenseTracker.Api.Services.Interfaces;

public interface ICurrentUserService
{
    Guid GetRequiredUserId();
}
