using Chat.Domain;

namespace Chat.Domain.EventMessages;

public class EventMessage
{
    public long Id { get; private set; }
    public long EventId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? EditedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    private EventMessage() { }

    public EventMessage(long eventId, Guid userId, string content, DateTimeOffset now)
    {
        if (eventId <= 0)
            throw new DomainException("Chat.InvalidEventId");

        if (userId == Guid.Empty)
            throw new DomainException("Chat.InvalidUserId");

        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Chat.EmptyMessage");

        EventId = eventId;
        UserId = userId;
        Content = content.Trim();
        CreatedAt = now;
        EditedAt = null;
        IsDeleted = false;
    }

    public void Edit(string content, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Chat.EmptyMessage");

        if (IsDeleted)
            throw new DomainException("Chat.MessageDeleted");

        Content = content.Trim();
        EditedAt = now;
    }

    public void Delete()
    {
        Content = string.Empty;
        IsDeleted = true;
    }
}
