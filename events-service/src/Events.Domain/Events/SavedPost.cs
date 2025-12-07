namespace Events.Domain.Events;

public class SavedPost
{
    public long Id { get; private set; }
    public Guid UserId { get; private set; }
    public long EventId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SavedPost() { }

    public SavedPost(Guid userId, long eventId, DateTimeOffset now)
    {
        UserId = userId;
        EventId = eventId;
        CreatedAt = now;
    }
}
