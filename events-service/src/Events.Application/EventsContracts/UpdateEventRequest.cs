using Events.Domain.Events;

namespace Events.Application.EventsContracts;

public sealed record UpdateEventRequest(
    string? Title,
    string? Description,
    string? City,
    string? Address,
    int? MaxMembers,
    int? Price,
    Payment? Payment,
    DateTimeOffset? EventStartAt,
    DateTimeOffset? EventEndAt,
    bool? IsRecurring,
    string? RecurrenceRule,
    string? CallLink,
    EventVisibility? Visibility);
