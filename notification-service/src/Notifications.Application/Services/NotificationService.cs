using Notifications.Application.Abstractions;
using Notifications.Application.Models;
using Notifications.Domain;
using Notifications.Domain.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IDateTimeProvider _clock;

    public NotificationService(INotificationRepository repository, IDateTimeProvider clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task CreateAsync(CreateNotificationRequest request, CancellationToken ct)
    {
        if (request.UserId == Guid.Empty) throw new DomainException("Notification.InvalidUserId");
        if (string.IsNullOrWhiteSpace(request.Type)) throw new DomainException("Notification.InvalidType");
        if (string.IsNullOrWhiteSpace(request.Payload)) throw new DomainException("Notification.InvalidPayload");

        var notification = new Notification(request.UserId, request.Type, request.Payload, _clock.UtcNow);
        await _repository.AddAsync(notification, ct);
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(Guid userId, bool onlyUnread, int page, int pageSize, CancellationToken ct)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var notifications = await _repository.GetForUserAsync(userId, onlyUnread, normalizedPage, normalizedPageSize, ct);
        return notifications.Select(Map).ToList();
    }

    public async Task MarkReadAsync(Guid userId, IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        if (ids == null || ids.Count == 0) return;
        await _repository.MarkReadAsync(userId, ids, _clock.UtcNow, ct);
    }

    private static NotificationResponse Map(Notification notification)
    {
        return new NotificationResponse(notification.Id, notification.UserId, notification.Type, notification.Payload, notification.IsRead, notification.CreatedAt, notification.ReadAt);
    }
}
