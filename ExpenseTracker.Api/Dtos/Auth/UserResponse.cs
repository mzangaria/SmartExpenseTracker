namespace ExpenseTracker.Api.Dtos.Auth;

public class UserResponse
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;
}
