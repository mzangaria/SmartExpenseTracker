using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Services.Interfaces;
using ExpenseTracker.Api.Services.Models;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Api.Services;

public class TelegramBotClient(
    HttpClient httpClient,
    IOptions<TelegramOptions> options,
    ILogger<TelegramBotClient> logger) : ITelegramBotClient
{
    private readonly TelegramOptions _options = options.Value;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long offset, int timeoutSeconds, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            return [];
        }

        var body = new
        {
            offset,
            timeout = timeoutSeconds,
            allowed_updates = new[] { "message" }
        };

        using var response = await PostAsync("getUpdates", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Telegram getUpdates failed with status code {StatusCode}.", response.StatusCode);
            return [];
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = JsonSerializer.Deserialize<TelegramApiResponse<List<TelegramUpdate>>>(responseBody, SerializerOptions);
        return payload?.Ok == true && payload.Result is not null ? payload.Result : [];
    }

    public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            return;
        }

        var body = new
        {
            chat_id = chatId,
            text
        };

        using var response = await PostAsync("sendMessage", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Telegram sendMessage failed with status code {StatusCode}.", response.StatusCode);
        }
    }

    private Task<HttpResponseMessage> PostAsync(string method, object body, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.telegram.org/bot{_options.BotToken}/{method}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(body, SerializerOptions), Encoding.UTF8, "application/json");
        return httpClient.SendAsync(request, cancellationToken);
    }

    private sealed class TelegramApiResponse<T>
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public T? Result { get; set; }
    }
}
