using Events.Domain.Events;

namespace Events.Application.EventsContracts;

public sealed record CreateEventRequest(
    string Title,
    string? Description,
    EventType Type,
    string City,
    string Address,
    EventVisibility Visibility,
    int? MaxMembers,
    int? Price,
    Payment? Payment,
    DateTimeOffset EventStartAt,
    DateTimeOffset? EventEndAt,
    bool IsRecurring,
    string? RecurrenceRule,
    string? CallLink);
