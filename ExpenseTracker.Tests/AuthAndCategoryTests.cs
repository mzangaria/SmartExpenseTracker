using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExpenseTracker.Api.Dtos.Auth;
using ExpenseTracker.Api.Dtos.Categories;
using ExpenseTracker.Api.Dtos.FinancialMessages;

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

        var inboxMessages = await client.GetFromJsonAsync<List<FinancialMessageResponse>>("/financial-messages");
        Assert.NotNull(inboxMessages);
        var welcomeMessage = Assert.Single(inboxMessages!);
        Assert.Equal("systeminsight", welcomeMessage.Type);
        Assert.Equal("unread", welcomeMessage.Status);
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

    [Fact]
    public async Task FinancialMessages_AreUserScoped_And_StatusChangesAreTracked()
    {
        using var ownerClient = factory.CreateClient();
        var ownerSession = await RegisterAsync(ownerClient, "owner-inbox@example.com");
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerSession.Token);

        using var secondClient = factory.CreateClient();
        var secondSession = await RegisterAsync(secondClient, "other-inbox@example.com");
        secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondSession.Token);

        var ownerMessages = await ownerClient.GetFromJsonAsync<List<FinancialMessageResponse>>("/financial-messages");
        var secondMessages = await secondClient.GetFromJsonAsync<List<FinancialMessageResponse>>("/financial-messages");

        Assert.NotNull(ownerMessages);
        Assert.NotNull(secondMessages);

        var ownerMessage = Assert.Single(ownerMessages!);
        var secondMessage = Assert.Single(secondMessages!);
        Assert.NotEqual(ownerMessage.Id, secondMessage.Id);

        var forbidden = await secondClient.PostAsync($"/financial-messages/{ownerMessage.Id}/archive", content: null);
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);

        var readResponse = await ownerClient.PostAsync($"/financial-messages/{ownerMessage.Id}/read", content: null);
        readResponse.EnsureSuccessStatusCode();

        var unreadCount = await ownerClient.GetFromJsonAsync<UnreadCountResponse>("/financial-messages/unread-count");
        Assert.NotNull(unreadCount);
        Assert.Equal(0, unreadCount!.Count);

        var readMessages = await ownerClient.GetFromJsonAsync<List<FinancialMessageResponse>>("/financial-messages?status=read");
        Assert.NotNull(readMessages);
        var updatedMessage = Assert.Single(readMessages!);
        Assert.Equal(ownerMessage.Id, updatedMessage.Id);
        Assert.NotNull(updatedMessage.ReadAtUtc);
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
