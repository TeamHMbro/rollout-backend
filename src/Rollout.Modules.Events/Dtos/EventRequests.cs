namespace Rollout.Modules.Events.Dtos;

public sealed class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PlaceName { get; set; }
    public string? Address { get; set; }
    public string? Category { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int MaxMembers { get; set; }
}

public sealed class UpdateEventRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? City { get; set; }
    public string? PlaceName { get; set; }
    public string? Address { get; set; }
    public string? Category { get; set; }
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public int? MaxMembers { get; set; }
}