using System.Net;
using System.Net.Http.Json;
using Rollout.IntegrationTests.Infrastructure;
using Xunit;

namespace Rollout.IntegrationTests.Users;

public sealed class UsersEndpointsTests
{
    [Fact]
    public async Task PatchMe_Then_GetMe_Returns_Updated_Profile()
    {
        using var factory = new RolloutApiFactory();
        using var anonymousClient = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";
        var tokens = await TestApi.RegisterAsync(anonymousClient, email);

        using var client = TestApi.CreateAuthorizedClient(factory, tokens.AccessToken);

        var patchResponse = await client.PatchAsJsonAsync("/users/me", new
        {
            Username = $"user_{Guid.NewGuid():N}".Substring(0, 14),
            DisplayName = "Al Tester",
            City = "Almaty",
            Bio = "Backend MVP",
            AvatarUrl = "https://example.com/avatar.png"
        });

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var getResponse = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var profile = await getResponse.Content.ReadFromJsonAsync<MyProfileResponse>();

        Assert.NotNull(profile);
        Assert.Equal("Al Tester", profile!.DisplayName);
        Assert.Equal("Almaty", profile.City);
        Assert.Equal("Backend MVP", profile.Bio);
        Assert.Equal("https://example.com/avatar.png", profile.AvatarUrl);
    }

    [Fact]
    public async Task GetMe_Without_Token_Returns_Unauthorized()
    {
        using var factory = new RolloutApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchMe_With_Duplicate_Username_Returns_Conflict()
    {
        using var factory = new RolloutApiFactory();
        using var anonymousClient = factory.CreateClient();

        var firstTokens = await TestApi.RegisterAsync(anonymousClient, $"user1-{Guid.NewGuid():N}@example.com");
        var secondTokens = await TestApi.RegisterAsync(anonymousClient, $"user2-{Guid.NewGuid():N}@example.com");

        using var firstClient = TestApi.CreateAuthorizedClient(factory, firstTokens.AccessToken);
        using var secondClient = TestApi.CreateAuthorizedClient(factory, secondTokens.AccessToken);

        var username = $"user_{Guid.NewGuid():N}".Substring(0, 14);

        await TestApi.EnsureProfileAsync(firstClient, username, "First User");

        var response = await secondClient.PatchAsJsonAsync("/users/me", new
        {
            Username = username,
            DisplayName = "Second User"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}