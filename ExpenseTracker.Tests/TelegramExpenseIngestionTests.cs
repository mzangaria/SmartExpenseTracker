using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos.Auth;
using ExpenseTracker.Api.Dtos.Categories;
using ExpenseTracker.Api.Dtos.Expenses;
using ExpenseTracker.Api.Dtos.Telegram;
using ExpenseTracker.Api.Services;
using ExpenseTracker.Api.Services.Interfaces;
using ExpenseTracker.Api.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Tests;

public class TelegramExpenseIngestionTests(ExpenseTrackerApiFactory factory) : IClassFixture<ExpenseTrackerApiFactory>
{
    [Fact]
    public async Task ConnectToken_CanLinkPrivateChat_AndStoresOnlyHash()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "telegram-link@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var tokenResponse = await client.PostAsync("/telegram/connect-token", null);
        tokenResponse.EnsureSuccessStatusCode();
        var connectToken = await tokenResponse.Content.ReadFromJsonAsync<TelegramConnectTokenResponse>();
        var token = ExtractStartToken(connectToken!.DeepLink);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.DoesNotContain(await dbContext.TelegramLinkTokens.Select(item => item.TokenHash).ToListAsync(), item => item == token);

        var bot = new FakeTelegramBotClient();
        var handler = BuildHandler(scope.ServiceProvider, bot);
        await handler.HandleAsync(BuildUpdate(100, "/start " + token), CancellationToken.None);
        await handler.HandleAsync(BuildUpdate(101, "/start " + token), CancellationToken.None);

        var status = await client.GetFromJsonAsync<TelegramStatusResponse>("/telegram/status");
        Assert.True(status!.IsConnected);
        Assert.Equal("Telegram is connected. Send an expense like: coffee 18", bot.Messages[0].Text);
        Assert.Equal("This link is invalid or expired. Generate a new link in the app.", bot.Messages[1].Text);
    }

    [Fact]
    public async Task DuplicateUpdate_DoesNotCreateDuplicateExpense()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAndLinkAsync(client, "telegram-duplicate@example.com");

        await using var scope = factory.Services.CreateAsyncScope();
        var bot = new FakeTelegramBotClient();
        var handler = BuildHandler(scope.ServiceProvider, bot);
        var update = BuildUpdate(200, "coffee 18");

        await handler.HandleAsync(update, CancellationToken.None);
        await handler.HandleAsync(update, CancellationToken.None);

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await dbContext.Expenses.CountAsync(item => item.UserId == session.User.Id));
        Assert.Equal(1, await dbContext.TelegramUpdatesProcessed.CountAsync(item => item.UpdateId == 200));
    }

    [Theory]
    [InlineData("coffee 18", "Food", 18)]
    [InlineData("spent 42 on lunch", "Food", 42)]
    [InlineData("uber 65 yesterday", "Transport", 65)]
    [InlineData("rent 3200 category rent", "Rent", 3200)]
    public async Task DeterministicParser_HandlesSupportedMessages(string text, string categoryName, decimal amount)
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, $"telegram-parser-{Guid.NewGuid()}@example.com");

        await using var scope = factory.Services.CreateAsyncScope();
        var parser = scope.ServiceProvider.GetRequiredService<IExpenseMessageParser>();
        var result = await parser.ParseAsync(session.User.Id, text, CancellationToken.None);

        Assert.True(result.ShouldSave);
        Assert.Equal(amount, result.Candidate.Amount);
        Assert.Equal(categoryName, result.Candidate.CategoryName);
        Assert.Equal("deterministic", result.Candidate.ParserType);
    }

    [Fact]
    public async Task AmbiguousMessage_AsksClarification_WithoutSaving()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAndLinkAsync(client, "telegram-ambiguous@example.com");

        await using var scope = factory.Services.CreateAsyncScope();
        var bot = new FakeTelegramBotClient();
        var handler = BuildHandler(scope.ServiceProvider, bot);
        await handler.HandleAsync(BuildUpdate(300, "coffee with friend"), CancellationToken.None);

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await dbContext.Expenses.CountAsync(item => item.UserId == session.User.Id));
        Assert.Contains(bot.Messages, item => item.Text.Contains("missing amount", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("clarification_required", await dbContext.ExpenseIngestionLogs.Where(item => item.UserId == session.User.Id).Select(item => item.Status).SingleAsync());
    }

    [Fact]
    public async Task LastAndUndo_WorkOnlyForTelegramCreatedExpense()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAndLinkAsync(client, "telegram-commands@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        var categories = await client.GetFromJsonAsync<List<CategoryResponse>>("/categories");
        var food = categories!.First(item => item.Name == "Food");
        await client.PostAsJsonAsync("/expenses", new ExpenseRequest
        {
            Description = "Manual",
            Amount = 9,
            Currency = "ILS",
            CategoryId = food.Id,
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        await using var scope = factory.Services.CreateAsyncScope();
        var bot = new FakeTelegramBotClient();
        var handler = BuildHandler(scope.ServiceProvider, bot);

        await handler.HandleAsync(BuildUpdate(400, "coffee 18"), CancellationToken.None);
        await handler.HandleAsync(BuildUpdate(401, "/last"), CancellationToken.None);
        await handler.HandleAsync(BuildUpdate(402, "/undo"), CancellationToken.None);

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(bot.Messages, item => item.Text.StartsWith("Last: 18 ILS", StringComparison.Ordinal));
        Assert.Contains(bot.Messages, item => item.Text == "Removed the last Telegram expense.");
        Assert.Equal(1, await dbContext.Expenses.CountAsync(item => item.UserId == session.User.Id));
        Assert.Equal("Manual", await dbContext.Expenses.Where(item => item.UserId == session.User.Id).Select(item => item.Description).SingleAsync());
    }

    [Fact]
    public async Task GeminiAdapter_UsesStructuredOutput_AndParsesJson()
    {
        var handler = new FakeGeminiHandler();
        var httpClient = new HttpClient(handler);
        var parser = new GeminiExpenseAiParser(
            httpClient,
            Options.Create(new GeminiOptions
            {
                ApiKey = "test-key",
                BaseUrl = "https://example.test/v1beta",
                ExpenseParsingModel = "gemini-test"
            }),
            NullLogger<GeminiExpenseAiParser>.Instance);

        var categories = new List<ExpenseTracker.Api.Entities.Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Food" }
        };
        var result = await parser.ParseAsync("coffee 18", categories, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(18, result!.Amount);
        Assert.Equal("Food", result.CategoryName);
        Assert.Equal("gemini", result.ParserType);
        Assert.Contains("responseSchema", handler.RequestBody);
        Assert.Contains("responseMimeType", handler.RequestBody);
        Assert.Contains("models/gemini-test:generateContent", handler.RequestUri);
    }

    [Fact]
    public async Task UnlinkedChat_IsRejected()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var bot = new FakeTelegramBotClient();
        var handler = BuildHandler(scope.ServiceProvider, bot);

        await handler.HandleAsync(BuildUpdate(500, "coffee 18", 777777, 888888), CancellationToken.None);

        Assert.Equal("This chat is not linked. Open the app and use Connect Telegram first.", bot.Messages.Single().Text);
    }

    private async Task<AuthResponse> RegisterAndLinkAsync(HttpClient client, string email)
    {
        var session = await RegisterAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        var tokenResponse = await client.PostAsync("/telegram/connect-token", null);
        tokenResponse.EnsureSuccessStatusCode();
        var connectToken = await tokenResponse.Content.ReadFromJsonAsync<TelegramConnectTokenResponse>();
        var token = ExtractStartToken(connectToken!.DeepLink);

        await using var scope = factory.Services.CreateAsyncScope();
        var handler = BuildHandler(scope.ServiceProvider, new FakeTelegramBotClient());
        await handler.HandleAsync(BuildUpdate(Random.Shared.Next(10_000, 99_999), "/start " + token), CancellationToken.None);
        return session;
    }

    private static ITelegramUpdateHandler BuildHandler(IServiceProvider serviceProvider, ITelegramBotClient botClient)
    {
        return new TelegramUpdateHandler(
            serviceProvider.GetRequiredService<AppDbContext>(),
            botClient,
            serviceProvider.GetRequiredService<ITelegramConnectionService>(),
            serviceProvider.GetRequiredService<IExpenseMessageParser>(),
            serviceProvider.GetRequiredService<IExpenseService>(),
            NullLogger<TelegramUpdateHandler>.Instance);
    }

    private static TelegramUpdate BuildUpdate(long updateId, string text, long telegramUserId = 123456, long telegramChatId = 654321)
    {
        return new TelegramUpdate
        {
            UpdateId = updateId,
            Message = new TelegramMessage
            {
                MessageId = updateId + 1000,
                Text = text,
                From = new TelegramUser { Id = telegramUserId, IsBot = false },
                Chat = new TelegramChat { Id = telegramChatId, Type = "private" }
            }
        };
    }

    private static string ExtractStartToken(string deepLink)
    {
        return Uri.UnescapeDataString(new Uri(deepLink).Query.TrimStart('?').Split("start=", StringSplitOptions.None)[1]);
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private sealed class FakeTelegramBotClient : ITelegramBotClient
    {
        public List<(long ChatId, string Text)> Messages { get; } = [];

        public Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long offset, int timeoutSeconds, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<TelegramUpdate>>([]);
        }

        public Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken)
        {
            Messages.Add((chatId, text));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGeminiHandler : HttpMessageHandler
    {
        public string RequestBody { get; private set; } = string.Empty;

        public string RequestUri { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri!.ToString();
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            const string payload = """
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [
                          {
                            "text": "{\"amount\":18,\"currency\":\"ILS\",\"category\":\"Food\",\"merchant\":\"coffee\",\"date\":\"2026-04-17\",\"note\":null,\"confidence\":0.92}"
                          }
                        ]
                      }
                    }
                  ]
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }
    }
}
