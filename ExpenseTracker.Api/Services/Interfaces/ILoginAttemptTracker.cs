namespace ExpenseTracker.Api.Services.Interfaces;

public interface ILoginAttemptTracker
{
    bool IsLockedOut(string email, string ipAddress, out TimeSpan retryAfter);

    int RecordFailure(string email, string ipAddress);

    void Reset(string email, string ipAddress);
}
