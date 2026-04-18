using System.Text.Json;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.Expenses;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Services.Interfaces;
using ExpenseTracker.Api.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Services;

public class TelegramUpdateHandler(
    AppDbContext dbContext,
    ITelegramBotClient botClient,
    ITelegramConnectionService connectionService,
    IExpenseMessageParser parser,
    IExpenseService expenseService,
    ILogger<TelegramUpdateHandler> logger) : ITelegramUpdateHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task HandleAsync(TelegramUpdate update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        if (message?.Chat is null || message.From is null || string.IsNullOrWhiteSpace(message.Text))
        {
            await TryMarkProcessedAsync(update, cancellationToken);
            return;
        }

        if (!await TryMarkProcessedAsync(update, cancellationToken))
        {
            return;
        }

        if (!string.Equals(message.Chat.Type, "private", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var text = message.Text.Trim();
        if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            await HandleStartAsync(text, message, cancellationToken);
            return;
        }

        var connection = await dbContext.TelegramConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.IsActive &&
                item.TelegramChatId == message.Chat.Id &&
                item.TelegramUserId == message.From.Id,
                cancellationToken);

        if (connection is null)
        {
            await botClient.SendMessageAsync(message.Chat.Id, "This chat is not linked. Open the app and use Connect Telegram first.", cancellationToken);
            await LogAsync(null, update, text, "none", null, null, "rejected", "Unlinked Telegram chat.", null, cancellationToken);
            return;
        }

        if (text.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendMessageAsync(message.Chat.Id, "Send examples like: coffee 18, spent 42 on lunch, uber 65 yesterday, rent 3200 category housing. Commands: /last, /undo, /help.", cancellationToken);
            return;
        }

        if (text.StartsWith("/last", StringComparison.OrdinalIgnoreCase))
        {
            await HandleLastAsync(connection, message.Chat.Id, cancellationToken);
            return;
        }

        if (text.StartsWith("/undo", StringComparison.OrdinalIgnoreCase))
        {
            await HandleUndoAsync(connection, message.Chat.Id, cancellationToken);
            return;
        }

        await HandleExpenseMessageAsync(connection, update, text, cancellationToken);
    }

    private async Task HandleStartAsync(string text, TelegramMessage message, CancellationToken cancellationToken)
    {
        var token = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token))
        {
            await botClient.SendMessageAsync(message.Chat!.Id, "Open the Telegram link from the app to connect this chat.", cancellationToken);
            return;
        }

        var linked = await connectionService.LinkAsync(token, message, cancellationToken);
        await botClient.SendMessageAsync(
            message.Chat!.Id,
            linked ? "Telegram is connected. Send an expense like: coffee 18" : "This link is invalid or expired. Generate a new link in the app.",
            cancellationToken);
    }

    private async Task HandleExpenseMessageAsync(TelegramConnection connection, TelegramUpdate update, string text, CancellationToken cancellationToken)
    {
        var parseResult = await parser.ParseAsync(connection.UserId, text, cancellationToken);
        var candidate = parseResult.Candidate;
        var parsedJson = JsonSerializer.Serialize(candidate, SerializerOptions);

        if (!parseResult.ShouldSave || candidate.Amount is null || candidate.Date is null || candidate.CategoryId is null)
        {
            await LogAsync(connection.UserId, update, text, candidate.ParserType, parsedJson, candidate.Confidence, "clarification_required", null, parseResult.ClarificationQuestion, cancellationToken);
            await botClient.SendMessageAsync(connection.TelegramChatId, parseResult.ClarificationQuestion ?? "Please send a clearer expense message.", cancellationToken);
            return;
        }

        try
        {
            var created = await expenseService.CreateAsync(connection.UserId, new ExpenseRequest
            {
                Description = candidate.Merchant ?? "Telegram expense",
                Amount = candidate.Amount.Value,
                Currency = "ILS",
                CategoryId = candidate.CategoryId.Value,
                ExpenseDate = candidate.Date.Value,
                Merchant = candidate.Merchant,
                Notes = candidate.Note,
                UseAiCategory = candidate.ParserType == "gemini"
            }, cancellationToken);

            await LogAsync(connection.UserId, update, text, candidate.ParserType, parsedJson, candidate.Confidence, "created", null, null, cancellationToken, created.Id);
            await botClient.SendMessageAsync(connection.TelegramChatId, $"Saved {created.Amount:0.##} ILS for {created.CategoryName} on {created.ExpenseDate:yyyy-MM-dd}.", cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Telegram expense save failed.");
            await LogAsync(connection.UserId, update, text, candidate.ParserType, parsedJson, candidate.Confidence, "failed", "Expense could not be saved.", null, cancellationToken);
            await botClient.SendMessageAsync(connection.TelegramChatId, "I could not save that expense. Please check the amount and category.", cancellationToken);
        }
    }

    private async Task HandleLastAsync(TelegramConnection connection, long chatId, CancellationToken cancellationToken)
    {
        var log = await dbContext.ExpenseIngestionLogs
            .AsNoTracking()
            .Where(item => item.UserId == connection.UserId && item.Channel == "telegram" && item.CreatedExpenseId != null && item.Status == "created")
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (log?.CreatedExpenseId is null)
        {
            await botClient.SendMessageAsync(chatId, "No Telegram expense has been saved yet.", cancellationToken);
            return;
        }

        var expense = await expenseService.GetByIdAsync(connection.UserId, log.CreatedExpenseId.Value, cancellationToken);
        if (expense is null)
        {
            await botClient.SendMessageAsync(chatId, "The last Telegram expense no longer exists.", cancellationToken);
            return;
        }

        await botClient.SendMessageAsync(chatId, $"Last: {expense.Amount:0.##} ILS for {expense.CategoryName} on {expense.ExpenseDate:yyyy-MM-dd} ({expense.Description}).", cancellationToken);
    }

    private async Task HandleUndoAsync(TelegramConnection connection, long chatId, CancellationToken cancellationToken)
    {
        var log = await dbContext.ExpenseIngestionLogs
            .Where(item => item.UserId == connection.UserId && item.Channel == "telegram" && item.CreatedExpenseId != null && item.Status == "created")
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (log?.CreatedExpenseId is null)
        {
            await botClient.SendMessageAsync(chatId, "There is no Telegram expense to undo.", cancellationToken);
            return;
        }

        var deleted = await expenseService.DeleteAsync(connection.UserId, log.CreatedExpenseId.Value, cancellationToken);
        log.Status = deleted ? "undone" : "undo_missing";
        log.ErrorMessage = deleted ? null : "Expense was already missing.";
        await dbContext.SaveChangesAsync(cancellationToken);

        await botClient.SendMessageAsync(chatId, deleted ? "Removed the last Telegram expense." : "The last Telegram expense was already gone.", cancellationToken);
    }

    private async Task<bool> TryMarkProcessedAsync(TelegramUpdate update, CancellationToken cancellationToken)
    {
        if (await dbContext.TelegramUpdatesProcessed.AnyAsync(item => item.UpdateId == update.UpdateId, cancellationToken))
        {
            return false;
        }

        MarkProcessed(update);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    private void MarkProcessed(TelegramUpdate update)
    {
        dbContext.TelegramUpdatesProcessed.Add(new TelegramUpdateProcessed
        {
            UpdateId = update.UpdateId,
            MessageId = update.Message?.MessageId,
            ChatId = update.Message?.Chat?.Id,
            ProcessedAtUtc = DateTime.UtcNow
        });
    }

    private async Task LogAsync(
        Guid? userId,
        TelegramUpdate update,
        string originalText,
        string parserType,
        string? parsedPayloadJson,
        decimal? confidence,
        string status,
        string? error,
        string? clarification,
        CancellationToken cancellationToken,
        Guid? createdExpenseId = null)
    {
        dbContext.ExpenseIngestionLogs.Add(new ExpenseIngestionLog
        {
            UserId = userId,
            Channel = "telegram",
            OriginalText = originalText.Length <= 2000 ? originalText : originalText[..2000],
            ParserType = parserType,
            ParsedPayloadJson = parsedPayloadJson,
            Confidence = confidence,
            Status = status,
            ErrorMessage = error,
            ClarificationQuestion = clarification,
            CreatedExpenseId = createdExpenseId,
            TelegramUpdateId = update.UpdateId,
            TelegramMessageId = update.Message?.MessageId,
            TelegramChatId = update.Message?.Chat?.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
