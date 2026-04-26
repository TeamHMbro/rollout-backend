namespace Rollout.IntegrationTests.Infrastructure;

internal sealed class AuthTokensResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

internal sealed class MeResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}

internal sealed class MyProfileResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}

internal sealed class EventUserSummaryResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

internal sealed class EventDetailsResponse
{
    public long EventId { get; set; }
    public Guid CreatorUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PlaceName { get; set; }
    public string? Address { get; set; }
    public string? Category { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int MembersCount { get; set; }
    public int MaxMembers { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsJoined { get; set; }
    public bool IsCreator { get; set; }
    public EventUserSummaryResponse Creator { get; set; } = new();
}

internal sealed class EventListItemResponse
{
    public long EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PlaceName { get; set; }
    public string? Category { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int MembersCount { get; set; }
    public int MaxMembers { get; set; }
    public int AvailableSpots { get; set; }
    public bool IsFull { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsJoined { get; set; }
    public bool IsCreator { get; set; }
}

internal sealed class EventMemberResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime JoinedAtUtc { get; set; }
}

internal sealed class PagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

internal sealed class ProblemResponse
{
    public int? Status { get; set; }
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public Dictionary<string, object>? Extensions { get; set; }
}

