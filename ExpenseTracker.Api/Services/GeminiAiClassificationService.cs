using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Dtos.Ai;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Api.Services;

public class GeminiAiClassificationService(
    HttpClient httpClient,
    ICategoryService categoryService,
    IOptions<GeminiOptions> options,
    ILogger<GeminiAiClassificationService> logger) : IAiClassificationService
{
    private readonly GeminiOptions _options = options.Value;

    public async Task<ClassifyExpenseResponse> ClassifyExpenseAsync(Guid userId, string description, CancellationToken cancellationToken)
    {
        var allowedCategories = await categoryService.GetAllowedCategoryEntitiesAsync(userId, cancellationToken);
        var categoryNames = allowedCategories.Select(category => category.Name).OrderBy(name => name).ToList();

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            logger.LogWarning("Gemini API key is missing.");
            return Failure();
        }

        var prompt = $"""
            You categorize personal expenses.
            Return exactly one category name from this list:
            {string.Join(", ", categoryNames)}

            Expense description: {description}

            Return plain text only.
            """;

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl}/models/{_options.Model}:generateContent?key={_options.ApiKey}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
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
                    }
                }),
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini classification failed with status code {StatusCode}.", response.StatusCode);
                return Failure();
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseBody);
            var suggestedName = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()
                ?.Trim();

            var match = allowedCategories.FirstOrDefault(category =>
                string.Equals(category.Name, suggestedName, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                logger.LogWarning("Gemini returned invalid category '{SuggestedName}'.", suggestedName);
                return Failure();
            }

            return new ClassifyExpenseResponse
            {
                Success = true,
                SuggestedCategory = match.Name,
                SuggestedCategoryId = match.Id
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Gemini classification request failed.");
            return Failure();
        }
    }

    private static ClassifyExpenseResponse Failure()
    {
        return new ClassifyExpenseResponse
        {
            Success = false,
            Message = "Could not suggest a category. Please choose one manually."
        };
    }
}
