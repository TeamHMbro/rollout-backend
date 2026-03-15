using System.Net;
using System.Net.Http.Json;
using Rollout.IntegrationTests.Infrastructure;
using Xunit;

namespace Rollout.IntegrationTests.Auth;

public sealed class AuthEndpointsTests
{
    [Fact]
    public async Task Register_Then_GetMe_Returns_Current_User()
    {
        using var factory = new RolloutApiFactory();
        using var anonymousClient = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";
        var tokens = await TestApi.RegisterAsync(anonymousClient, email);

        using var authorizedClient = TestApi.CreateAuthorizedClient(factory, tokens.AccessToken);

        var response = await authorizedClient.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await response.Content.ReadFromJsonAsync<MeResponse>();

        Assert.NotNull(me);
        Assert.Equal(email, me!.Email);
        Assert.NotEqual(Guid.Empty, me.UserId);
    }

    [Fact]
    public async Task Register_With_Duplicate_Email_Returns_Conflict()
    {
        using var factory = new RolloutApiFactory();
        using var client = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";
        await TestApi.RegisterAsync(client, email);

        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_With_Invalid_Password_Returns_Unauthorized()
    {
        using var factory = new RolloutApiFactory();
        using var client = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";
        await TestApi.RegisterAsync(client, email);

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            Email = email,
            Password = "WrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_With_Invalid_Token_Returns_Unauthorized()
    {
        using var factory = new RolloutApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/refresh", new
        {
            RefreshToken = "invalid-refresh-token"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_Without_Token_Returns_Unauthorized()
    {
        using var factory = new RolloutApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}