using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExpenseTracker.Api.Dtos.Auth;
using ExpenseTracker.Api.Dtos.Categories;

namespace ExpenseTracker.Tests;

public class AuthAndCategoryTests(ExpenseTrackerApiFactory factory) : IClassFixture<ExpenseTrackerApiFactory>
{
    [Fact]
    public async Task Register_AutoLogsIn_And_ReturnsProfile()
    {
        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.Equal("user@example.com", auth.User.Email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var meResponse = await client.GetAsync("/auth/me");

        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(me);
        Assert.Equal(auth.User.Id, me.Id);
    }

    [Fact]
    public async Task CustomCategories_AreUserScoped()
    {
        using var ownerClient = factory.CreateClient();
        var ownerSession = await RegisterAsync(ownerClient, "owner@example.com");
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerSession.Token);

        var createResponse = await ownerClient.PostAsJsonAsync("/categories", new CreateCategoryRequest
        {
            Name = "Pets"
        });
        createResponse.EnsureSuccessStatusCode();

        using var secondClient = factory.CreateClient();
        var secondSession = await RegisterAsync(secondClient, "other@example.com");
        secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondSession.Token);

        var ownerCategories = await ownerClient.GetFromJsonAsync<List<CategoryResponse>>("/categories");
        var secondCategories = await secondClient.GetFromJsonAsync<List<CategoryResponse>>("/categories");

        Assert.Contains(ownerCategories!, category => category.Name == "Pets");
        Assert.DoesNotContain(secondCategories!, category => category.Name == "Pets");
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
