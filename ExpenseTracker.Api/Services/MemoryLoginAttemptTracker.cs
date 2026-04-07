using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ExpenseTracker.Api.Services;

public class MemoryLoginAttemptTracker(IMemoryCache memoryCache) : ILoginAttemptTracker
{
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int LockoutThreshold = 5;

    public bool IsLockedOut(string email, string ipAddress, out TimeSpan retryAfter)
    {
        var attempt = memoryCache.Get<LoginAttemptState>(BuildKey(email, ipAddress));
        if (attempt?.LockoutEndUtc is null || attempt.LockoutEndUtc <= DateTimeOffset.UtcNow)
        {
            retryAfter = TimeSpan.Zero;
            return false;
        }

        retryAfter = attempt.LockoutEndUtc.Value - DateTimeOffset.UtcNow;
        return true;
    }

    public int RecordFailure(string email, string ipAddress)
    {
        var cacheKey = BuildKey(email, ipAddress);
        var attempt = memoryCache.Get<LoginAttemptState>(cacheKey) ?? new LoginAttemptState();
        attempt.FailureCount++;

        if (attempt.FailureCount >= LockoutThreshold)
        {
            attempt.LockoutEndUtc = DateTimeOffset.UtcNow.Add(LockoutDuration);
        }

        memoryCache.Set(cacheKey, attempt, FailureWindow);
        return attempt.FailureCount;
    }

    public void Reset(string email, string ipAddress)
    {
        memoryCache.Remove(BuildKey(email, ipAddress));
    }

    private static string BuildKey(string email, string ipAddress)
    {
        return $"login-attempt:{email}:{ipAddress}";
    }

    private sealed class LoginAttemptState
    {
        public int FailureCount { get; set; }

        public DateTimeOffset? LockoutEndUtc { get; set; }
    }
}
