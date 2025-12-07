namespace Events.Domain.Events;

public class LikedPost
{
    public long Id { get; private set; }
    public Guid UserId { get; private set; }
    public long EventId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private LikedPost() { }

    public LikedPost(Guid userId, long eventId, DateTimeOffset now)
    {
        UserId = userId;
        EventId = eventId;
        CreatedAt = now;
    }
}
