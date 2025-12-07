using Notifications.Domain;
using System;

namespace Notifications.Domain.Notifications;

public class Notification
{
    public long Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; }
    public string Payload { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private Notification()
    {
    }

    public Notification(Guid userId, string type, string payload, DateTimeOffset now)
    {
        if (userId == Guid.Empty) throw new DomainException("Notification.InvalidUserId");
        if (string.IsNullOrWhiteSpace(type)) throw new DomainException("Notification.InvalidType");
        if (string.IsNullOrWhiteSpace(payload)) throw new DomainException("Notification.InvalidPayload");

        UserId = userId;
        Type = type.Trim();
        Payload = payload.Trim();
        CreatedAt = now;
        IsRead = false;
        ReadAt = null;
    }

    public void MarkAsRead(DateTimeOffset now)
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = now;
    }
}
