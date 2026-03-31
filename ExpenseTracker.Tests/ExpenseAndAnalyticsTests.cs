using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExpenseTracker.Api.Dtos.Ai;
using ExpenseTracker.Api.Dtos.Analytics;
using ExpenseTracker.Api.Dtos.Auth;
using ExpenseTracker.Api.Dtos.Categories;
using ExpenseTracker.Api.Dtos.Expenses;

namespace ExpenseTracker.Tests;

public class ExpenseAndAnalyticsTests(ExpenseTrackerApiFactory factory) : IClassFixture<ExpenseTrackerApiFactory>
{
    [Fact]
    public async Task Expenses_CanBeCreated_Filtered_AndSummarized()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "analytics@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var categories = await client.GetFromJsonAsync<List<CategoryResponse>>("/categories");
        var food = categories!.First(category => category.Name == "Food");

        await client.PostAsJsonAsync("/expenses", new ExpenseRequest
        {
            Description = "Lunch",
            Amount = 45.50m,
            Currency = "ILS",
            CategoryId = food.Id,
            ExpenseDate = new DateOnly(2026, 3, 10),
            Notes = "Campus lunch",
            Merchant = "Cafe",
            UseAiCategory = false
        });

        await client.PostAsJsonAsync("/expenses", new ExpenseRequest
        {
            Description = "Dinner",
            Amount = 70m,
            Currency = "ILS",
            CategoryId = food.Id,
            ExpenseDate = new DateOnly(2026, 3, 18),
            UseAiCategory = true
        });

        var filteredExpenses = await client.GetFromJsonAsync<List<ExpenseResponse>>("/expenses?year=2026&month=3&search=Dinner");
        Assert.Single(filteredExpenses!);
        Assert.Equal("Dinner", filteredExpenses![0].Description);
        Assert.Equal("ai", filteredExpenses[0].CategorySource);

        var summary = await client.GetFromJsonAsync<MonthlySummaryResponse>("/analytics/monthly-summary?year=2026&month=3");
        Assert.NotNull(summary);
        Assert.Equal(115.50m, summary!.TotalSpent);
        Assert.Equal(2, summary.NumberOfExpenses);

        var breakdown = await client.GetFromJsonAsync<List<CategoryBreakdownResponse>>("/analytics/category-breakdown?year=2026&month=3");
        Assert.NotNull(breakdown);
        Assert.Contains(breakdown!, item => item.CategoryName == "Food" && item.TotalAmount == 115.50m);
    }

    [Fact]
    public async Task AiEndpoint_FailsGracefully_WithoutConfiguredKey()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "ai@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var response = await client.PostAsJsonAsync("/ai/classify-expense", new ClassifyExpenseRequest
        {
            Description = "Uber ride to campus"
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ClassifyExpenseResponse>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("Could not suggest a category. Please choose one manually.", result.Message);
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
}
