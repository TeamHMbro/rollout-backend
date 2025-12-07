using System;

namespace Notifications.Application.Models;

public sealed record NotificationResponse(long Id, Guid UserId, string Type, string Payload, bool IsRead, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);
