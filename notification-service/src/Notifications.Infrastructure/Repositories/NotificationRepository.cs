using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Domain.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _db;

    public NotificationRepository(NotificationsDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Notification notification, CancellationToken ct)
    {
        await _db.Notifications.AddAsync(notification, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Notification>> GetForUserAsync(Guid userId, bool onlyUnread, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Notifications.AsNoTracking().Where(x => x.UserId == userId);
        if (onlyUnread)
        {
            query = query.Where(x => !x.IsRead);
        }

        var skip = (page - 1) * pageSize;
        return await query.OrderByDescending(x => x.CreatedAt).Skip(skip).Take(pageSize).ToListAsync(ct);
    }

    public async Task MarkReadAsync(Guid userId, IReadOnlyCollection<long> ids, DateTimeOffset now, CancellationToken ct)
    {
        var notifications = await _db.Notifications.Where(x => x.UserId == userId && ids.Contains(x.Id) && !x.IsRead).ToListAsync(ct);
        foreach (var notification in notifications)
        {
            notification.MarkAsRead(now);
        }

        await _db.SaveChangesAsync(ct);
    }
}
