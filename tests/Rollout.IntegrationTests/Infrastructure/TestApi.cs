using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Rollout.IntegrationTests.Infrastructure;

internal static class TestApi
{
    public static async Task<AuthTokensResponse> RegisterAsync(HttpClient client, string email, string password = "Password123!")
    {
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthTokensResponse>())!;
    }

    public static async Task<AuthTokensResponse> LoginAsync(HttpClient client, string email, string password = "Password123!")
    {
        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthTokensResponse>())!;
    }

    public static async Task<long> CreateEventAsync(
        HttpClient client,
        string title = "Board Games Night",
        string city = "Almaty",
        int maxMembers = 5,
        DateTime? startAtUtc = null,
        DateTime? endAtUtc = null)
    {
        var start = startAtUtc ?? DateTime.UtcNow.AddDays(2);
        var end = endAtUtc ?? start.AddHours(2);

        var response = await client.PostAsJsonAsync("/events", new
        {
            Title = title,
            Description = "Play board games together",
            City = city,
            PlaceName = "Anticafe",
            Address = "Abylai Khan 10",
            Category = "games",
            StartAtUtc = start,
            EndAtUtc = end,
            MaxMembers = maxMembers
        });

        response.EnsureSuccessStatusCode();

        var eventResponse = (await response.Content.ReadFromJsonAsync<EventDetailsResponse>())!;
        return eventResponse.EventId;
    }

    public static async Task EnsureProfileAsync(
        HttpClient client,
        string username,
        string displayName,
        string city = "Almaty")
    {
        var response = await client.PatchAsJsonAsync("/users/me", new
        {
            Username = username,
            DisplayName = displayName,
            City = city
        });

        response.EnsureSuccessStatusCode();
    }

    public static HttpClient CreateAuthorizedClient(RolloutApiFactory factory, string accessToken)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}