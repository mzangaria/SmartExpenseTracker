using System.Globalization;
using System.Text.RegularExpressions;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Services.Interfaces;
using ExpenseTracker.Api.Services.Models;

namespace ExpenseTracker.Api.Services;

public class ExpenseMessageParser(
    ICategoryService categoryService,
    IExpenseAiParser aiParser,
    ILogger<ExpenseMessageParser> logger) : IExpenseMessageParser
{
    private const decimal HighConfidenceThreshold = 0.80m;
    private static readonly Regex AmountRegex = new(@"(?<!\d)(\d+(?:[.,]\d{1,2})?)(?:\s*(?:ils|nis|₪))?(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ExplicitCategoryRegex = new(@"\bcategory\s+([A-Za-z][A-Za-z\s-]{1,60})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SpentOnRegex = new(@"\bspent\s+\d+(?:[.,]\d{1,2})?(?:\s*(?:ils|nis|₪))?\s+on\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Dictionary<string, string[]> CategoryKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Food"] = ["food", "coffee", "lunch", "dinner", "breakfast", "sushi", "restaurant", "pizza", "burger", "groceries"],
        ["Transport"] = ["uber", "taxi", "bus", "train", "ride", "fuel", "parking"],
        ["Bills"] = ["bill", "electricity", "internet", "water", "phone"],
        ["Shopping"] = ["shopping", "clothes", "mall", "amazon", "store"],
        ["Entertainment"] = ["movie", "cinema", "netflix", "spotify", "game", "concert"],
        ["Health"] = ["doctor", "pharmacy", "medicine", "clinic", "health"],
        ["Education"] = ["course", "tuition", "book", "school", "university"],
        ["Rent"] = ["rent", "landlord", "apartment", "lease", "housing"]
    };

    public async Task<ExpenseParseResult> ParseAsync(Guid userId, string text, CancellationToken cancellationToken)
    {
        var allowedCategories = await categoryService.GetAllowedCategoryEntitiesAsync(userId, cancellationToken);
        var deterministic = BuildDeterministicCandidate(text, allowedCategories);
        if (CanSave(deterministic))
        {
            return BuildResult(deterministic);
        }

        try
        {
            var aiCandidate = await aiParser.ParseAsync(text, allowedCategories, cancellationToken);
            if (aiCandidate is not null)
            {
                NormalizeCandidate(aiCandidate, text, allowedCategories);
                if (CanSave(aiCandidate))
                {
                    return BuildResult(aiCandidate);
                }

                return BuildResult(aiCandidate);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Expense AI parser failed. Falling back to clarification.");
        }

        return BuildResult(deterministic);
    }

    private static ParsedExpenseCandidate BuildDeterministicCandidate(string text, IReadOnlyList<Category> allowedCategories)
    {
        var normalized = NormalizeWhitespace(text);
        var amount = TryParseAmount(normalized);
        var date = ParseDate(normalized);
        var explicitCategory = ParseExplicitCategory(normalized);
        var category = ResolveCategory(explicitCategory, normalized, allowedCategories);
        var merchant = ParseMerchant(normalized, explicitCategory);

        var candidate = new ParsedExpenseCandidate
        {
            Amount = amount,
            Date = date,
            CategoryId = category?.Id,
            CategoryName = category?.Name,
            Merchant = Sanitize(merchant, 200),
            Note = null,
            Confidence = CalculateConfidence(amount, category, explicitCategory, normalized),
            ParserType = "deterministic"
        };

        NormalizeCandidate(candidate, normalized, allowedCategories);
        return candidate;
    }

    private static ExpenseParseResult BuildResult(ParsedExpenseCandidate candidate)
    {
        candidate.MissingFields = GetMissingFields(candidate);
        candidate.ClarificationQuestion = candidate.MissingFields.Count == 0
            ? "Please confirm the expense with a clearer category or amount."
            : $"Please send the missing {string.Join(", ", candidate.MissingFields)} for this expense.";

        return new ExpenseParseResult
        {
            Candidate = candidate,
            ShouldSave = CanSave(candidate),
            ClarificationQuestion = candidate.ClarificationQuestion
        };
    }

    private static bool CanSave(ParsedExpenseCandidate candidate)
    {
        return candidate.Amount is > 0
            && candidate.Date is not null
            && candidate.CategoryId is not null
            && candidate.Confidence >= HighConfidenceThreshold;
    }

    private static void NormalizeCandidate(ParsedExpenseCandidate candidate, string originalText, IReadOnlyList<Category> allowedCategories)
    {
        candidate.Currency = "ILS";
        candidate.Merchant = Sanitize(candidate.Merchant, 200) ?? BuildDescription(originalText);
        candidate.Note = Sanitize(candidate.Note, 1000);
        candidate.Date ??= DateOnly.FromDateTime(DateTime.UtcNow);

        if (candidate.CategoryId is null && !string.IsNullOrWhiteSpace(candidate.CategoryName))
        {
            var category = ResolveCategory(candidate.CategoryName, originalText, allowedCategories);
            candidate.CategoryId = category?.Id;
            candidate.CategoryName = category?.Name;
        }

        candidate.MissingFields = GetMissingFields(candidate);
    }

    private static List<string> GetMissingFields(ParsedExpenseCandidate candidate)
    {
        var missing = new List<string>();
        if (candidate.Amount is null or <= 0)
        {
            missing.Add("amount");
        }

        if (candidate.Date is null)
        {
            missing.Add("date");
        }

        if (candidate.CategoryId is null)
        {
            missing.Add("category");
        }

        return missing;
    }

    private static decimal CalculateConfidence(decimal? amount, Category? category, string? explicitCategory, string text)
    {
        if (amount is null || category is null)
        {
            return 0.35m;
        }

        if (!string.IsNullOrWhiteSpace(explicitCategory))
        {
            return 0.95m;
        }

        return CategoryKeywords.TryGetValue(category.Name, out var keywords) &&
               keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            ? 0.88m
            : 0.75m;
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
        return decimal.TryParse(candidate.Groups[1].Value.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) && amount > 0
            ? decimal.Round(amount, 2)
            : null;
    }

    private static DateOnly ParseDate(string text)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (text.Contains("yesterday", StringComparison.OrdinalIgnoreCase))
        {
            return today.AddDays(-1);
        }

        return today;
    }

    private static string? ParseExplicitCategory(string text)
    {
        var match = ExplicitCategoryRegex.Match(text);
        return match.Success ? NormalizeWhitespace(match.Groups[1].Value) : null;
    }

    private static Category? ResolveCategory(string? categoryText, string fullText, IReadOnlyList<Category> allowedCategories)
    {
        if (!string.IsNullOrWhiteSpace(categoryText))
        {
            var exact = allowedCategories.FirstOrDefault(category =>
                string.Equals(category.Name, categoryText.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                return exact;
            }

            if (categoryText.Contains("housing", StringComparison.OrdinalIgnoreCase))
            {
                return allowedCategories.FirstOrDefault(category => category.Name == "Rent");
            }
        }

        var categoryByName = allowedCategories.FirstOrDefault(category =>
            fullText.Contains(category.Name, StringComparison.OrdinalIgnoreCase));
        if (categoryByName is not null)
        {
            return categoryByName;
        }

        foreach (var category in allowedCategories)
        {
            if (CategoryKeywords.TryGetValue(category.Name, out var keywords) &&
                keywords.Any(keyword => fullText.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return category;
            }
        }

        return null;
    }

    private static string? ParseMerchant(string text, string? explicitCategory)
    {
        var spentOn = SpentOnRegex.Match(text);
        var merchant = spentOn.Success ? spentOn.Groups[1].Value : text;
        if (!string.IsNullOrWhiteSpace(explicitCategory))
        {
            merchant = ExplicitCategoryRegex.Replace(merchant, string.Empty);
        }

        merchant = Regex.Replace(merchant, @"\b(spent|on|today|yesterday)\b", string.Empty, RegexOptions.IgnoreCase);
        merchant = AmountRegex.Replace(merchant, string.Empty);
        return BuildDescription(merchant);
    }

    private static string BuildDescription(string text)
    {
        var description = NormalizeWhitespace(text);
        return string.IsNullOrWhiteSpace(description) ? "Telegram expense" : description;
    }

    private static string? Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = NormalizeWhitespace(value);
        sanitized = Regex.Replace(sanitized, @"[\p{C}&&[^\r\n\t]]", string.Empty);
        return sanitized.Length <= maxLength ? sanitized : sanitized[..maxLength];
    }

    private static string NormalizeWhitespace(string text)
    {
        return Regex.Replace(text.Trim(), @"\s+", " ");
    }
}
