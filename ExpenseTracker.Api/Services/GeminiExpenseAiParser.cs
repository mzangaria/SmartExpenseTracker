using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Services.Interfaces;
using ExpenseTracker.Api.Services.Models;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Api.Services;

public class GeminiExpenseAiParser(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiExpenseAiParser> logger) : IExpenseAiParser
{
    private readonly GeminiOptions _options = options.Value;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<ParsedExpenseCandidate?> ParseAsync(string text, IReadOnlyList<Category> allowedCategories, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            logger.LogInformation("Gemini expense parser skipped because API key is missing.");
            return null;
        }

        var categoryNames = allowedCategories.Select(category => category.Name).OrderBy(name => name).ToArray();
        var prompt = $"""
            Extract one personal expense from one Telegram message.
            Return only JSON matching the response schema.
            Allowed categories: {string.Join(", ", categoryNames)}.
            Currency must be ILS.
            Do not invent missing amount, date, or category. Use null fields and low confidence when unclear.
            Dates must be yyyy-MM-dd. Today is {DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}.
            Message: {text}
            """;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0,
                maxOutputTokens = 256,
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        amount = new { type = "NUMBER", nullable = true },
                        currency = new { type = "STRING", nullable = true },
                        category = new { type = "STRING", nullable = true },
                        merchant = new { type = "STRING", nullable = true },
                        date = new { type = "STRING", nullable = true },
                        note = new { type = "STRING", nullable = true },
                        confidence = new { type = "NUMBER" }
                    },
                    required = new[] { "amount", "currency", "category", "merchant", "date", "note", "confidence" }
                }
            }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/models/{_options.ExpenseParsingModel}:generateContent");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("x-goog-api-key", _options.ApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody, SerializerOptions), Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini expense parsing failed with status code {StatusCode}.", response.StatusCode);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseBody);
            var jsonText = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return null;
            }

            var parsed = JsonSerializer.Deserialize<GeminiExpenseParseResponse>(jsonText, SerializerOptions);
            if (parsed is null)
            {
                return null;
            }

            return new ParsedExpenseCandidate
            {
                Amount = parsed.Amount,
                Currency = "ILS",
                CategoryName = parsed.Category,
                Merchant = parsed.Merchant,
                Date = DateOnly.TryParse(parsed.Date, out var date) ? date : null,
                Note = parsed.Note,
                Confidence = Math.Clamp((decimal)parsed.Confidence, 0m, 1m),
                ParserType = "gemini"
            };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Gemini expense parsing request failed.");
            return null;
        }
    }

    private sealed class GeminiExpenseParseResponse
    {
        public decimal? Amount { get; set; }

        public string? Currency { get; set; }

        public string? Category { get; set; }

        public string? Merchant { get; set; }

        public string? Date { get; set; }

        public string? Note { get; set; }

        public double Confidence { get; set; }
    }
}
