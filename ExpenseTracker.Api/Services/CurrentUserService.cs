using ExpenseTracker.Api.Extensions;
using ExpenseTracker.Api.Services.Interfaces;

namespace ExpenseTracker.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid GetRequiredUserId()
    {
        var principal = httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("No active HTTP context.");

        return principal.GetRequiredUserId();
    }
}
