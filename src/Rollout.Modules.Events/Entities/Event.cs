namespace Rollout.Modules.Events.Entities;

public sealed class Event
{
    public long Id { get; set; }
    public Guid CreatorUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PlaceName { get; set; }
    public string? Address { get; set; }
    public string? Category { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int MaxMembers { get; set; }
    public EventStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    public ICollection<EventMember> Members { get; set; } = new List<EventMember>();
}