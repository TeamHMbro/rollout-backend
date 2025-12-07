using Events.Application.Abstractions;
using Events.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Events.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.Repositories;

public sealed class EventRepository : IEventRepository
{
    private readonly EventDbContext _db;

    public EventRepository(EventDbContext db)
    {
        _db = db;
    }

    public Task<EventEntity?> GetByIdAsync(long id, CancellationToken ct)
    {
        return _db.Events.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task AddAsync(EventEntity entity, CancellationToken ct)
    {
        _db.Events.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(EventEntity entity, CancellationToken ct)
    {
        _db.Events.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EventEntity>> GetFeedAsync(
        string? city,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _db.Events.AsQueryable();

        query = query.Where(e => e.Status == EventStatus.Published);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(e => e.City == city);

        if (from.HasValue)
            query = query.Where(e => e.EventStartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.EventStartAt <= to.Value);

        query = query.OrderBy(e => e.EventStartAt);

        var skip = (page - 1) * pageSize;

        return await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EventEntity>> GetByOwnerAsync(Guid ownerId, CancellationToken ct)
    {
        return await _db.Events
            .Where(e => e.OwnerId == ownerId)
            .OrderByDescending(e => e.EventStartAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EventEntity>> GetGoingByUserAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Events
            .Where(e => e.Status == EventStatus.Published)
            .Join(
                _db.EventMembers.Where(m => m.UserId == userId && m.Status == RsvpStatus.Going),
                e => e.Id,
                m => m.EventId,
                (e, m) => e)
            .OrderBy(e => e.EventStartAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EventEntity>> GetSavedByUserAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Events
            .Where(e => e.Status == EventStatus.Published)
            .Join(
                _db.SavedPosts.Where(s => s.UserId == userId),
                e => e.Id,
                s => s.EventId,
                (e, s) => e)
            .OrderBy(e => e.EventStartAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EventEntity>> GetLikedByUserAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Events
            .Where(e => e.Status == EventStatus.Published)
            .Join(
                _db.LikedPosts.Where(l => l.UserId == userId),
                e => e.Id,
                l => l.EventId,
                (e, l) => e)
            .OrderByDescending(e => e.EventStartAt)
            .ToListAsync(ct);
    }

    public async Task<bool> TryIncrementMembersAsync(long eventId, DateTimeOffset now, CancellationToken ct)
    {
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"
    UPDATE events
    SET members_count = members_count + 1,
        updated_at    = {now}
    WHERE id = {eventId}
    AND status = 'Published'
    AND event_start_at > {now}
    AND (max_members IS NULL OR members_count < max_members);
    ", ct);

        return affected == 1;
    }

    public async Task DecrementMembersAsync(long eventId, DateTimeOffset now, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"
    UPDATE events
    SET members_count = CASE WHEN members_count > 0 THEN members_count - 1 ELSE 0 END,
        updated_at    = {now}
    WHERE id = {eventId};
    ", ct);
    }
}
