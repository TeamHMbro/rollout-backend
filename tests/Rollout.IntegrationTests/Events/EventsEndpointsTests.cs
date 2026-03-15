using System.Net;
using System.Net.Http.Json;
using Rollout.IntegrationTests.Infrastructure;
using Xunit;

namespace Rollout.IntegrationTests.Events;

public sealed class EventsEndpointsTests
{
    [Fact]
    public async Task Create_Join_Members_And_Calendar_Flow_Works()
    {
        using var factory = new RolloutApiFactory();
        using var anonymousClient = factory.CreateClient();

        var ownerTokens = await TestApi.RegisterAsync(anonymousClient, $"owner-{Guid.NewGuid():N}@example.com");
        var memberTokens = await TestApi.RegisterAsync(anonymousClient, $"member-{Guid.NewGuid():N}@example.com");

        using var ownerClient = TestApi.CreateAuthorizedClient(factory, ownerTokens.AccessToken);
        using var memberClient = TestApi.CreateAuthorizedClient(factory, memberTokens.AccessToken);

        await TestApi.EnsureProfileAsync(ownerClient, $"owner_{Guid.NewGuid():N}".Substring(0, 14), "Owner User");
        await TestApi.EnsureProfileAsync(memberClient, $"member_{Guid.NewGuid():N}".Substring(0, 15), "Member User");

        var eventId = await TestApi.CreateEventAsync(ownerClient);

        var createdEventResponse = await ownerClient.GetAsync($"/events/{eventId}");
        var createdEvent = await createdEventResponse.Content.ReadFromJsonAsync<EventDetailsResponse>();

        Assert.NotNull(createdEvent);
        Assert.True(createdEvent!.IsCreator);
        Assert.True(createdEvent.IsJoined);
        Assert.Equal(1, createdEvent.MembersCount);

        var joinResponse = await memberClient.PostAsync($"/events/{eventId}/join", null);

        Assert.Equal(HttpStatusCode.NoContent, joinResponse.StatusCode);

        var membersResponse = await memberClient.GetAsync($"/events/{eventId}/members");

        Assert.Equal(HttpStatusCode.OK, membersResponse.StatusCode);

        var members = await membersResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<EventMemberResponse>>();

        Assert.NotNull(members);
        Assert.Equal(2, members!.Count);

        var joinedResponse = await memberClient.GetAsync("/events/me/joined?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, joinedResponse.StatusCode);

        var joined = await joinedResponse.Content.ReadFromJsonAsync<PagedResponse<EventListItemResponse>>();

        Assert.NotNull(joined);
        Assert.Contains(joined!.Items, x => x.EventId == eventId);

        var calendarResponse = await memberClient.GetAsync("/events/me/calendar?status=upcoming&page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, calendarResponse.StatusCode);

        var calendar = await calendarResponse.Content.ReadFromJsonAsync<PagedResponse<EventListItemResponse>>();

        Assert.NotNull(calendar);
        Assert.Contains(calendar!.Items, x => x.EventId == eventId);

        var cancelResponse = await ownerClient.PostAsync($"/events/{eventId}/cancel", null);

        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var calendarAfterCancelResponse = await memberClient.GetAsync("/events/me/calendar?status=upcoming&page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, calendarAfterCancelResponse.StatusCode);

        var calendarAfterCancel = await calendarAfterCancelResponse.Content.ReadFromJsonAsync<PagedResponse<EventListItemResponse>>();

        Assert.NotNull(calendarAfterCancel);
        Assert.DoesNotContain(calendarAfterCancel!.Items, x => x.EventId == eventId);
    }

    [Fact]
    public async Task Create_Without_Token_Returns_Unauthorized()
    {
        using var factory = new RolloutApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/events", new
        {
            Title = "Board Games Night",
            Description = "Play board games together",
            City = "Almaty",
            PlaceName = "Anticafe",
            Address = "Abylai Khan 10",
            Category = "games",
            StartAtUtc = DateTime.UtcNow.AddDays(2),
            EndAtUtc = DateTime.UtcNow.AddDays(2).AddHours(2),
            MaxMembers = 5
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_With_Invalid_TimeRange_Returns_BadRequest()
    {
        using var factory = new RolloutApiFactory();
        using var anonymousClient = factory.CreateClient();

        var tokens = await TestApi.RegisterAsync(anonymousClient, $"owner-{Guid.NewGuid():N}@example.com");
        using var client = TestApi.CreateAuthorizedClient(factory, tokens.AccessToken);

        var startAtUtc = DateTime.UtcNow.AddDays(2);
        var endAtUtc = startAtUtc.AddHours(-1);

        var response = await client.PostAsJsonAsync("/events", new
        {
            Title = "Broken Event",
            Description = "Wrong time range",
            City = "Almaty",
            PlaceName = "Anticafe",
            Address = "Abylai Khan 10",
            Category = "games",
            StartAtUtc = startAtUtc,
            EndAtUtc = endAtUtc,
            MaxMembers = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_By_NonCreator_Returns_Forbidden()
    {
        using var factory = new RolloutApiFactory();
        using var anonymousClient = factory.CreateClient();

        var ownerTokens = await TestApi.RegisterAsync(anonymousClient, $"owner-{Guid.NewGuid():N}@example.com");
        var otherTokens = await TestApi.RegisterAsync(anonymousClient, $"other-{Guid.NewGuid():N}@example.com");

        using var ownerClient = TestApi.CreateAuthorizedClient(factory, ownerTokens.AccessToken);
        using var otherClient = TestApi.CreateAuthorizedClient(factory, otherTokens.AccessToken);

        var eventId = await TestApi.CreateEventAsync(ownerClient);

        var response = await otherClient.PatchAsJsonAsync($"/events/{eventId}", new
        {
            Title = "Hacked Title"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Join_Cancelled_Event_Returns_BadRequest()
    {
        using var factory = new RolloutApiFactory();
        using var anonymousClient = factory.CreateClient();

        var ownerTokens = await TestApi.RegisterAsync(anonymousClient, $"owner-{Guid.NewGuid():N}@example.com");
        var memberTokens = await TestApi.RegisterAsync(anonymousClient, $"member-{Guid.NewGuid():N}@example.com");

        using var ownerClient = TestApi.CreateAuthorizedClient(factory, ownerTokens.AccessToken);
        using var memberClient = TestApi.CreateAuthorizedClient(factory, memberTokens.AccessToken);

        var eventId = await TestApi.CreateEventAsync(ownerClient);

        await ownerClient.PostAsync($"/events/{eventId}/cancel", null);

        var response = await memberClient.PostAsync($"/events/{eventId}/join", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Join_Full_Event_Returns_Conflict()
    {
        using var factory = new RolloutApiFactory();
        using var anonymousClient = factory.CreateClient();

        var ownerTokens = await TestApi.RegisterAsync(anonymousClient, $"owner-{Guid.NewGuid():N}@example.com");
        var memberTokens = await TestApi.RegisterAsync(anonymousClient, $"member-{Guid.NewGuid():N}@example.com");

        using var ownerClient = TestApi.CreateAuthorizedClient(factory, ownerTokens.AccessToken);
        using var memberClient = TestApi.CreateAuthorizedClient(factory, memberTokens.AccessToken);

        var eventId = await TestApi.CreateEventAsync(ownerClient, maxMembers: 1);

        var response = await memberClient.PostAsync($"/events/{eventId}/join", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Creator_Cannot_Leave_Own_Event()
    {
        using var factory = new RolloutApiFactory();
        using var anonymousClient = factory.CreateClient();

        var ownerTokens = await TestApi.RegisterAsync(anonymousClient, $"owner-{Guid.NewGuid():N}@example.com");
        using var ownerClient = TestApi.CreateAuthorizedClient(factory, ownerTokens.AccessToken);

        var eventId = await TestApi.CreateEventAsync(ownerClient);

        var response = await ownerClient.DeleteAsync($"/events/{eventId}/join");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Calendar_Past_Returns_Past_Joined_Event()
    {
        using var factory = new RolloutApiFactory();
        using var anonymousClient = factory.CreateClient();

        var ownerTokens = await TestApi.RegisterAsync(anonymousClient, $"owner-{Guid.NewGuid():N}@example.com");
        var memberTokens = await TestApi.RegisterAsync(anonymousClient, $"member-{Guid.NewGuid():N}@example.com");

        using var ownerClient = TestApi.CreateAuthorizedClient(factory, ownerTokens.AccessToken);
        using var memberClient = TestApi.CreateAuthorizedClient(factory, memberTokens.AccessToken);

        var startAtUtc = DateTime.UtcNow.AddDays(-3);
        var endAtUtc = DateTime.UtcNow.AddDays(-3).AddHours(2);

        var eventId = await TestApi.CreateEventAsync(ownerClient, title: "Past Event", startAtUtc: startAtUtc, endAtUtc: endAtUtc);

        var joinResponse = await memberClient.PostAsync($"/events/{eventId}/join", null);

        Assert.Equal(HttpStatusCode.NoContent, joinResponse.StatusCode);

        var response = await memberClient.GetAsync("/events/me/calendar?status=past&page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var calendar = await response.Content.ReadFromJsonAsync<PagedResponse<EventListItemResponse>>();

        Assert.NotNull(calendar);
        Assert.Contains(calendar!.Items, x => x.EventId == eventId);
    }
}