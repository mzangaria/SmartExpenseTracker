using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ExpenseTracker.Api.Dtos.Ai;
using ExpenseTracker.Api.Dtos.Analytics;
using ExpenseTracker.Api.Dtos.Auth;
using ExpenseTracker.Api.Dtos.Budgets;
using ExpenseTracker.Api.Dtos.Categories;
using ExpenseTracker.Api.Dtos.Expenses;
using ExpenseTracker.Api.Dtos.Imports;
using Microsoft.AspNetCore.Mvc;

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
        Assert.Equal("ILS", summary.Currency);

        var breakdown = await client.GetFromJsonAsync<List<CategoryBreakdownResponse>>("/analytics/category-breakdown?year=2026&month=3");
        Assert.NotNull(breakdown);
        Assert.Contains(breakdown!, item => item.CategoryName == "Food" && item.TotalAmount == 115.50m);
    }

    [Fact]
    public async Task Expenses_AreNormalizedToIls_And_SearchRemainsCaseInsensitive()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "currency@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var categories = await client.GetFromJsonAsync<List<CategoryResponse>>("/categories");
        var food = categories!.First(category => category.Name == "Food");

        var createResponse = await client.PostAsJsonAsync("/expenses", new ExpenseRequest
        {
            Description = "Late Dinner",
            Amount = 22.75m,
            Currency = "usd",
            CategoryId = food.Id,
            ExpenseDate = new DateOnly(2026, 3, 20),
            UseAiCategory = false
        });

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>();

        Assert.NotNull(created);
        Assert.Equal("ILS", created!.Currency);

        var filteredExpenses = await client.GetFromJsonAsync<List<ExpenseResponse>>("/expenses?year=2026&month=3&search=dInNeR");
        Assert.NotNull(filteredExpenses);
        var filteredExpense = Assert.Single(filteredExpenses);
        Assert.Equal("ILS", filteredExpense.Currency);
    }

    [Fact]
    public async Task ExpenseCreate_ReturnsValidationProblem_ForInvalidCategory()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "invalid-expense-category@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var response = await client.PostAsJsonAsync("/expenses", new ExpenseRequest
        {
            Description = "Invalid category expense",
            Amount = 20m,
            Currency = "ILS",
            CategoryId = Guid.NewGuid(),
            ExpenseDate = new DateOnly(2026, 4, 7),
            UseAiCategory = false
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("CategoryId", problem!.Errors.Keys, StringComparer.OrdinalIgnoreCase);
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
        Assert.False(result?.Success ?? true);
        Assert.Equal("Could not suggest a category. Please choose one manually.", result?.Message);
    }

    [Fact]
    public async Task ParseExpense_ExtractsAmountDateMerchant_AndKnownCategory()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "parse1@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var response = await client.PostAsJsonAsync("/ai/parse-expense", new ParseExpenseRequest
        {
            Text = "Spent 42 ILS on sushi yesterday at Japanika"
        });

        response.EnsureSuccessStatusCode();
        var parsed = await response.Content.ReadFromJsonAsync<ParseExpenseResponse>();

        Assert.NotNull(parsed);
        Assert.True(parsed!.Success);
        Assert.Equal(42m, parsed.Draft.Amount);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), parsed.Draft.ExpenseDate);
        Assert.Equal("Japanika", parsed.Draft.Merchant);
        Assert.NotNull(parsed.Draft.CategoryId);
        Assert.Equal("Food", parsed.Draft.CategoryName);
    }

    [Fact]
    public async Task ParseExpense_ReturnsPartialDraft_WhenAmountMissing()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "parse2@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var response = await client.PostAsJsonAsync("/ai/parse-expense", new ParseExpenseRequest
        {
            Text = "coffee with friend"
        });

        response.EnsureSuccessStatusCode();
        var parsed = await response.Content.ReadFromJsonAsync<ParseExpenseResponse>();

        Assert.NotNull(parsed);
        Assert.False(parsed!.Success);
        Assert.Contains("amount", parsed.MissingFields);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), parsed.Draft.ExpenseDate);
        Assert.Contains("Date defaulted to today.", parsed.Warnings);
    }

    [Fact]
    public async Task ParseExpense_CanResolveCustomCategory_Deterministically()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "parse3@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        await client.PostAsJsonAsync("/categories", new CreateCategoryRequest
        {
            Name = "Gym"
        });

        var response = await client.PostAsJsonAsync("/ai/parse-expense", new ParseExpenseRequest
        {
            Text = "gym membership 150 yesterday"
        });

        response.EnsureSuccessStatusCode();
        var parsed = await response.Content.ReadFromJsonAsync<ParseExpenseResponse>();

        Assert.NotNull(parsed);
        Assert.Equal("Gym", parsed!.Draft.CategoryName);
        Assert.Equal(150m, parsed.Draft.Amount);
    }

    [Fact]
    public async Task Budgets_CanBeManaged_AndVarianceThresholdsAreCalculated()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "budget@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var categories = await client.GetFromJsonAsync<List<CategoryResponse>>("/categories");
        Assert.NotNull(categories);
        var food = categories.First(category => category.Name == "Food");
        var transport = categories.First(category => category.Name == "Transport");

        var budgetUpsert = await client.PutAsJsonAsync($"/budgets/{food.Id}", new BudgetRequest { Amount = 100m });
        budgetUpsert.EnsureSuccessStatusCode();

        await client.PutAsJsonAsync($"/budgets/{transport.Id}", new BudgetRequest { Amount = 50m });

        await client.PostAsJsonAsync("/expenses", new ExpenseRequest
        {
            Description = "Groceries",
            Amount = 84m,
            Currency = "ILS",
            CategoryId = food.Id,
            ExpenseDate = new DateOnly(2026, 4, 10),
            UseAiCategory = false
        });

        await client.PostAsJsonAsync("/expenses", new ExpenseRequest
        {
            Description = "Taxi",
            Amount = 60m,
            Currency = "ILS",
            CategoryId = transport.Id,
            ExpenseDate = new DateOnly(2026, 4, 11),
            UseAiCategory = false
        });

        var budgets = await client.GetFromJsonAsync<List<BudgetResponse>>("/budgets");
        Assert.NotNull(budgets);
        Assert.Equal(2, budgets!.Count);

        var variance = await client.GetFromJsonAsync<List<BudgetVarianceResponse>>("/analytics/budget-variance?year=2026&month=4");
        Assert.NotNull(variance);
        Assert.Equal(2, variance!.Count);
        Assert.Equal("over_budget", variance[0].Status);
        Assert.Equal("warning", variance[1].Status);
        Assert.Equal(120m, variance[0].UsagePercent);
        Assert.Equal(84m, variance[1].UsagePercent);
        Assert.True(variance[0].ShowInWarningStrip);

        var deleteResponse = await client.DeleteAsync($"/budgets/{transport.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var updatedVariance = await client.GetFromJsonAsync<List<BudgetVarianceResponse>>("/analytics/budget-variance?year=2026&month=4");
        Assert.NotNull(updatedVariance);
        var remainingBudget = Assert.Single(updatedVariance);
        Assert.Equal(food.Id, remainingBudget.CategoryId);
    }

    [Fact]
    public async Task BudgetUpsert_ReturnsValidationProblem_ForInvalidCategory()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "invalid-budget-category@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var response = await client.PutAsJsonAsync($"/budgets/{Guid.NewGuid()}", new BudgetRequest
        {
            Amount = 100m
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("categoryId", problem!.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Expenses_CanBeExported_And_Imported_AsCsv()
    {
        using var exportClient = factory.CreateClient();
        var exportSession = await RegisterAsync(exportClient, "csv-expenses-export@example.com");
        exportClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", exportSession.Token);

        var categories = await exportClient.GetFromJsonAsync<List<CategoryResponse>>("/categories");
        Assert.NotNull(categories);
        var food = categories!.First(category => category.Name == "Food");

        var createResponse = await exportClient.PostAsJsonAsync("/expenses", new ExpenseRequest
        {
            Description = "Lunch, with team",
            Amount = 55.25m,
            Currency = "ILS",
            CategoryId = food.Id,
            ExpenseDate = new DateOnly(2026, 4, 8),
            Merchant = "Cafe 101",
            Notes = "Quoted \"note\"",
            UseAiCategory = false
        });
        createResponse.EnsureSuccessStatusCode();

        var exportResponse = await exportClient.GetAsync("/expenses/export/csv");
        exportResponse.EnsureSuccessStatusCode();
        var csv = await exportResponse.Content.ReadAsStringAsync();

        Assert.Contains("Description,Amount,Currency,ExpenseDate,CategoryName,Merchant,Notes,CategorySource", csv);
        Assert.Contains("\"Lunch, with team\"", csv);
        Assert.Contains("\"Quoted \"\"note\"\"\"", csv);

        using var importClient = factory.CreateClient();
        var importSession = await RegisterAsync(importClient, "csv-expenses-import@example.com");
        importClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", importSession.Token);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "expenses.csv");

        var importResponse = await importClient.PostAsync("/expenses/import/csv", content);
        importResponse.EnsureSuccessStatusCode();
        var importResult = await importResponse.Content.ReadFromJsonAsync<CsvImportResult>();

        Assert.NotNull(importResult);
        Assert.Equal(1, importResult!.ImportedCount);
        Assert.Empty(importResult.Errors);

        var importedExpenses = await importClient.GetFromJsonAsync<List<ExpenseResponse>>("/expenses");
        Assert.NotNull(importedExpenses);
        var importedExpense = Assert.Single(importedExpenses!);
        Assert.Equal("Lunch, with team", importedExpense.Description);
        Assert.Equal(55.25m, importedExpense.Amount);
        Assert.Equal("Food", importedExpense.CategoryName);
        Assert.Equal("Cafe 101", importedExpense.Merchant);
        Assert.Equal("Quoted \"note\"", importedExpense.Notes);
    }

    [Fact]
    public async Task Expenses_ImportCsv_ReturnsStructuredError_ForMalformedCsv()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "csv-expenses-malformed@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        const string csv = "Description,Amount,Currency,ExpenseDate,CategoryName\r\n\"Broken,12.00,ILS,2026-04-08,Food";

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "expenses.csv");

        var response = await client.PostAsync("/expenses/import/csv", content);
        response.EnsureSuccessStatusCode();

        var importResult = await response.Content.ReadFromJsonAsync<CsvImportResult>();
        Assert.NotNull(importResult);
        Assert.Equal(0, importResult!.ImportedCount);

        var error = Assert.Single(importResult.Errors);
        Assert.Equal(1, error.RowNumber);
        Assert.Contains("Invalid CSV format:", error.Message);
        Assert.Contains("unterminated quoted value", error.Message);
    }

    [Fact]
    public async Task Budgets_CanBeExported_And_Imported_AsCsv()
    {
        using var exportClient = factory.CreateClient();
        var exportSession = await RegisterAsync(exportClient, "csv-budgets-export@example.com");
        exportClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", exportSession.Token);

        var categories = await exportClient.GetFromJsonAsync<List<CategoryResponse>>("/categories");
        Assert.NotNull(categories);
        var food = categories!.First(category => category.Name == "Food");
        var transport = categories.First(category => category.Name == "Transport");

        (await exportClient.PutAsJsonAsync($"/budgets/{food.Id}", new BudgetRequest { Amount = 100m })).EnsureSuccessStatusCode();
        (await exportClient.PutAsJsonAsync($"/budgets/{transport.Id}", new BudgetRequest { Amount = 80m })).EnsureSuccessStatusCode();

        var exportResponse = await exportClient.GetAsync("/budgets/export/csv");
        exportResponse.EnsureSuccessStatusCode();
        var csv = await exportResponse.Content.ReadAsStringAsync();

        Assert.Contains("CategoryName,Amount", csv);
        Assert.Contains("Food,100.00", csv);
        Assert.Contains("Transport,80.00", csv);

        using var importClient = factory.CreateClient();
        var importSession = await RegisterAsync(importClient, "csv-budgets-import@example.com");
        importClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", importSession.Token);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "budgets.csv");

        var importResponse = await importClient.PostAsync("/budgets/import/csv", content);
        importResponse.EnsureSuccessStatusCode();
        var importResult = await importResponse.Content.ReadFromJsonAsync<CsvImportResult>();

        Assert.NotNull(importResult);
        Assert.Equal(2, importResult!.ImportedCount);
        Assert.Empty(importResult.Errors);

        var importedBudgets = await importClient.GetFromJsonAsync<List<BudgetResponse>>("/budgets");
        Assert.NotNull(importedBudgets);
        Assert.Equal(2, importedBudgets!.Count);
        Assert.Contains(importedBudgets, budget => budget.CategoryName == "Food" && budget.Amount == 100m);
        Assert.Contains(importedBudgets, budget => budget.CategoryName == "Transport" && budget.Amount == 80m);
    }

    [Fact]
    public async Task Budgets_ImportCsv_ReturnsStructuredError_ForMalformedCsv()
    {
        using var client = factory.CreateClient();
        var session = await RegisterAsync(client, "csv-budgets-malformed@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        const string csv = "CategoryName,Amount\r\n\"Food,100.00";

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "budgets.csv");

        var response = await client.PostAsync("/budgets/import/csv", content);
        response.EnsureSuccessStatusCode();

        var importResult = await response.Content.ReadFromJsonAsync<CsvImportResult>();
        Assert.NotNull(importResult);
        Assert.Equal(0, importResult!.ImportedCount);

        var error = Assert.Single(importResult.Errors);
        Assert.Equal(1, error.RowNumber);
        Assert.Contains("Invalid CSV format:", error.Message);
        Assert.Contains("unterminated quoted value", error.Message);
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
