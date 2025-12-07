using Events.Domain.Events;

namespace Events.Application.EventsContracts;

public sealed record EventResponse(
    long Id,
    Guid OwnerId,
    string Title,
    string? Description,
    EventType Type,
    string City,
    string Address,
    EventVisibility Visibility,
    EventStatus Status,
    int? MaxMembers,
    int MembersCount,
    int? Price,
    Payment? Payment,
    DateTimeOffset EventStartAt,
    DateTimeOffset? EventEndAt,
    bool IsRecurring,
    string? RecurrenceRule,
    string? CallLink);
