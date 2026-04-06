using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.Auth;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Extensions;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("auth")]
// AuthController handles registration, login, and "who am I" queries for the current session.
public class AuthController(
    AppDbContext dbContext,
    PasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        // Emails are normalized so login and uniqueness checks behave consistently.
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var exists = await dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            ModelState.AddModelError(nameof(request.Email), "Email already exists.");
            return ValidationProblem(ModelState);
        }

        var user = new User
        {
            Email = normalizedEmail,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(jwtTokenService.CreateToken(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Email == normalizedEmail, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "Wrong credentials." });
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Wrong credentials." });
        }

        return Ok(jwtTokenService.CreateToken(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        // The current user id comes from the authenticated JWT claims.
        var userId = User.GetRequiredUserId(); //automatically available from HttpContext (HttpContext.User).
        /*
            userId comes from the JWT token, which is authenticated by the [Authorize] attribute.
            Before controller action executes, the framework validates the JWT token,
            and if valid, it populates HttpContext.User with the claims from the token.
            In this case, we have a claim of type ClaimTypes.NameIdentifier which contains the user id as a string.
        */
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UserResponse
        {
            Id = user.Id,
            Email = user.Email
        });
    }
}
