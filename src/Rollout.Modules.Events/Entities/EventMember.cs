namespace Rollout.Modules.Events.Entities;

public sealed class EventMember
{
    public long EventId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAtUtc { get; set; }

    public Event Event { get; set; } = null!;
}