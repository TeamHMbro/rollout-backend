using Notifications.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications.Application.Services;

public interface INotificationService
{
    Task CreateAsync(CreateNotificationRequest request, CancellationToken ct);
    Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(Guid userId, bool onlyUnread, int page, int pageSize, CancellationToken ct);
    Task MarkReadAsync(Guid userId, IReadOnlyCollection<long> ids, CancellationToken ct);
}
