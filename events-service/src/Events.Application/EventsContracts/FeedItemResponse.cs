using Events.Domain.Events;

namespace Events.Application.EventsContracts;

public sealed record FeedItemResponse(
    long Id,
    Guid OwnerId,
    string Title,
    string City,
    string Address,
    EventType Type,
    EventVisibility Visibility,
    DateTimeOffset EventStartAt,
    DateTimeOffset? EventEndAt,
    int MembersCount,
    int? MaxMembers);
