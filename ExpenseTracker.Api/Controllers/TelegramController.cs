using ExpenseTracker.Api.Dtos.Telegram;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("telegram")]
public class TelegramController(
    ICurrentUserService currentUserService,
    ITelegramConnectionService telegramConnectionService) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<TelegramStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        return Ok(await telegramConnectionService.GetStatusAsync(userId, cancellationToken));
    }

    [HttpPost("connect-token")]
    public async Task<ActionResult<TelegramConnectTokenResponse>> CreateConnectToken(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        try
        {
            return Ok(await telegramConnectionService.CreateConnectTokenAsync(userId, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("connection")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        await telegramConnectionService.DisconnectAsync(userId, cancellationToken);
        return NoContent();
    }
}
