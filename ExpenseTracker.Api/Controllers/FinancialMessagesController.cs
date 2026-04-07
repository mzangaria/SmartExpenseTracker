using ExpenseTracker.Api.Dtos.FinancialMessages;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("financial-messages")]
public class FinancialMessagesController(
    ICurrentUserService currentUserService,
    IFinancialMessageService financialMessageService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FinancialMessageResponse>>> GetMessages([FromQuery] FinancialMessageQueryParameters query, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var messages = await financialMessageService.GetMessagesAsync(userId, query, cancellationToken);
        return Ok(messages);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountResponse>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var count = await financialMessageService.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(new UnreadCountResponse { Count = count });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<FinancialMessageResponse>> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var message = await financialMessageService.MarkReadAsync(userId, id, cancellationToken);
        return message is null ? NotFound() : Ok(message);
    }

    [HttpPost("{id:guid}/dismiss")]
    public async Task<ActionResult<FinancialMessageResponse>> Dismiss(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var message = await financialMessageService.DismissAsync(userId, id, cancellationToken);
        return message is null ? NotFound() : Ok(message);
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<FinancialMessageResponse>> Archive(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var message = await financialMessageService.ArchiveAsync(userId, id, cancellationToken);
        return message is null ? NotFound() : Ok(message);
    }
}
