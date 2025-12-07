using System;

namespace Notifications.Application.Models;

public sealed record CreateNotificationRequest(Guid UserId, string Type, string Payload);
