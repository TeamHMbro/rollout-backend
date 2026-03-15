namespace Rollout.Modules.Events.Dtos;

public sealed class PagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class EventUserSummaryResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public sealed class EventListItemResponse
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
    public string Status { get; set; } = string.Empty;
    public bool IsJoined { get; set; }
    public bool IsCreator { get; set; }
    public EventUserSummaryResponse Creator { get; set; } = new();
}

public sealed class EventDetailsResponse
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
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public EventUserSummaryResponse Creator { get; set; } = new();
}

public sealed class EventMemberResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime JoinedAtUtc { get; set; }
}