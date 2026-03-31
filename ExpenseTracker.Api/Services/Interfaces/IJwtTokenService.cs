using ExpenseTracker.Api.Dtos.Auth;
using ExpenseTracker.Api.Entities;

namespace ExpenseTracker.Api.Services.Interfaces;

public interface IJwtTokenService
{
    AuthResponse CreateToken(User user);
}
