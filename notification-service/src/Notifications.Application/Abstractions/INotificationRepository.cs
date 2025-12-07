using Notifications.Domain.Notifications;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct);
    Task<IReadOnlyList<Notification>> GetForUserAsync(Guid userId, bool onlyUnread, int page, int pageSize, CancellationToken ct);
    Task MarkReadAsync(Guid userId, IReadOnlyCollection<long> ids, DateTimeOffset now, CancellationToken ct);
}
