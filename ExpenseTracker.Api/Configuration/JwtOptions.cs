namespace ExpenseTracker.Api.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ExpenseTracker.Api";

    public string Audience { get; set; } = "ExpenseTracker.Client";

    public string Key { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 120;
}
