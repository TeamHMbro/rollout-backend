namespace Events.Domain.Events;

public class EventMember
{
    public long Id { get; private set; }
    public long EventId { get; private set; }
    public Guid UserId { get; private set; }
    public RsvpStatus Status { get; private set; }
    public ParticipantRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private EventMember() { }

    public EventMember(long eventId, Guid userId, ParticipantRole role, DateTimeOffset now)
    {
        EventId = eventId;
        UserId = userId;
        Status = RsvpStatus.Going;
        Role = role;
        JoinedAt = now;
    }

    public void Rejoin(DateTimeOffset now)
    {
        Status = RsvpStatus.Going;
        JoinedAt = now;
    }

    public void Leave()
    {
        Status = RsvpStatus.Declined;
    }
}
