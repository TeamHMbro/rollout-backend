using Events.Domain.Events;

namespace Events.Application.EventsContracts;

public sealed record EventMemberResponse(
    Guid UserId,
    string UserName,
    string? Avatar,
    RsvpStatus Status,
    ParticipantRole Role,
    DateTimeOffset JoinedAt);
