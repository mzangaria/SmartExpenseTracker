using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Dtos.Ai;
using ExpenseTracker.Api.Entities;
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
    private static readonly Regex AmountRegex = new(@"(?<!\d)(\d+(?:[.,]\d{1,2})?)(?:\s*(?:ils|nis|₪))?(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Dictionary<string, string[]> CategoryKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Food"] = ["food", "coffee", "lunch", "dinner", "breakfast", "sushi", "restaurant", "pizza", "burger", "groceries"],
        ["Transport"] = ["uber", "taxi", "bus", "train", "ride", "fuel", "parking"],
        ["Bills"] = ["bill", "electricity", "internet", "water", "phone"],
        ["Shopping"] = ["shopping", "clothes", "mall", "amazon", "store"],
        ["Entertainment"] = ["movie", "cinema", "netflix", "spotify", "game", "concert"],
        ["Health"] = ["doctor", "pharmacy", "medicine", "clinic", "health"],
        ["Education"] = ["course", "tuition", "book", "school", "university"],
        ["Rent"] = ["rent", "landlord", "apartment", "lease"]
    };

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

    public async Task<ParseExpenseResponse> ParseExpenseAsync(Guid userId, string text, CancellationToken cancellationToken)
    {
        var normalizedText = NormalizeWhitespace(text);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var warnings = new List<string>();
        var missingFields = new List<string>();
        var allowedCategories = await categoryService.GetAllowedCategoryEntitiesAsync(userId, cancellationToken);

        var parsedAmount = TryParseAmount(normalizedText);
        var parsedDate = ParseDate(normalizedText, today, warnings);
        var merchant = ParseMerchant(normalizedText);
        var category = await InferCategoryAsync(userId, normalizedText, allowedCategories, cancellationToken);

        if (parsedAmount is null)
        {
            missingFields.Add("amount");
        }

        if (category is null)
        {
            missingFields.Add("categoryId");
        }

        if (!ContainsExplicitDate(normalizedText))
        {
            warnings.Add("Date defaulted to today.");
        }

        var response = new ParseExpenseResponse
        {
            Success = parsedAmount is not null && category is not null,
            Draft = new ParsedExpenseDraftResponse
            {
                Description = BuildDescription(normalizedText, merchant),
                Amount = parsedAmount,
                ExpenseDate = parsedDate,
                CategoryId = category?.Id,
                CategoryName = category?.Name,
                Merchant = merchant,
                Notes = null,
                UseAiCategory = category is not null
            },
            MissingFields = missingFields,
            Warnings = warnings
        };

        if (!response.Success)
        {
            response.Message = missingFields.Count == 0
                ? "Could not fully parse the expense. Review the draft before saving."
                : "The expense was parsed partially. Complete the missing fields before saving.";
        }

        return response;
    }

    private static ClassifyExpenseResponse Failure()
    {
        return new ClassifyExpenseResponse
        {
            Success = false,
            Message = "Could not suggest a category. Please choose one manually."
        };
    }

    private async Task<Category?> InferCategoryAsync(Guid userId, string description, IReadOnlyList<Category> allowedCategories, CancellationToken cancellationToken)
    {
        var deterministicMatch = allowedCategories.FirstOrDefault(category =>
            description.Contains(category.Name, StringComparison.OrdinalIgnoreCase));

        if (deterministicMatch is not null)
        {
            return deterministicMatch;
        }

        foreach (var allowedCategory in allowedCategories)
        {
            if (!CategoryKeywords.TryGetValue(allowedCategory.Name, out var keywords))
            {
                continue;
            }

            if (keywords.Any(keyword => description.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return allowedCategory;
            }
        }

        var classification = await ClassifyExpenseAsync(userId, description, cancellationToken);
        if (!classification.Success || classification.SuggestedCategoryId is null)
        {
            return null;
        }

        return allowedCategories.FirstOrDefault(category => category.Id == classification.SuggestedCategoryId.Value);
    }

    private static decimal? TryParseAmount(string text)
    {
        var matches = AmountRegex.Matches(text);
        if (matches.Count == 0)
        {
            return null;
        }

        var explicitCurrencyMatch = matches
            .Cast<Match>()
            .FirstOrDefault(match => Regex.IsMatch(match.Value, "(ils|nis|₪)", RegexOptions.IgnoreCase));

        var candidate = explicitCurrencyMatch ?? matches[^1];
        return decimal.TryParse(candidate.Groups[1].Value.Replace(",", "."), out var amount) && amount > 0
            ? decimal.Round(amount, 2)
            : null;
    }

    private static DateOnly ParseDate(string text, DateOnly today, List<string> warnings)
    {
        if (text.Contains("yesterday", StringComparison.OrdinalIgnoreCase))
        {
            return today.AddDays(-1);
        }

        if (text.Contains("today", StringComparison.OrdinalIgnoreCase))
        {
            return today;
        }

        return today;
    }

    private static bool ContainsExplicitDate(string text)
    {
        return text.Contains("today", StringComparison.OrdinalIgnoreCase)
            || text.Contains("yesterday", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ParseMerchant(string text)
    {
        var marker = Regex.Match(text, @"\b(?:at|from)\s+([A-Za-z0-9&'""\-\s]+)$", RegexOptions.IgnoreCase);
        return marker.Success ? NormalizeWhitespace(marker.Groups[1].Value) : null;
    }

    private static string BuildDescription(string text, string? merchant)
    {
        var description = text;
        if (!string.IsNullOrWhiteSpace(merchant))
        {
            description = Regex.Replace(description, $@"\b(?:at|from)\s+{Regex.Escape(merchant)}$", string.Empty, RegexOptions.IgnoreCase);
        }

        description = Regex.Replace(description, @"\b(today|yesterday)\b", string.Empty, RegexOptions.IgnoreCase);
        description = Regex.Replace(description, @"(?<!\d)(\d+(?:[.,]\d{1,2})?)(?:\s*(?:ils|nis|₪))?(?!\d)", string.Empty, RegexOptions.IgnoreCase);
        description = NormalizeWhitespace(description);

        return string.IsNullOrWhiteSpace(description) ? text : description;
    }

    private static string NormalizeWhitespace(string text)
    {
        return Regex.Replace(text.Trim(), @"\s+", " ");
    }
}
