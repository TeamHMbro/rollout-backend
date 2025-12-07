using Events.Application.Abstractions;
using Events.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.Repositories;

public sealed class EventMemberRepository : IEventMemberRepository
{
    private readonly EventDbContext _db;

    public EventMemberRepository(EventDbContext db)
    {
        _db = db;
    }

    public Task<EventMember?> GetAsync(long eventId, Guid userId, CancellationToken ct)
    {
        return _db.EventMembers.FirstOrDefaultAsync(
            x => x.EventId == eventId && x.UserId == userId,
            ct);
    }

    public async Task AddAsync(EventMember member, CancellationToken ct)
    {
        _db.EventMembers.Add(member);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(EventMember member, CancellationToken ct)
    {
        _db.EventMembers.Update(member);
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountByEventAsync(long eventId, CancellationToken ct)
    {
        return _db.EventMembers.CountAsync(x => x.EventId == eventId && x.Status == RsvpStatus.Going, ct);
    }

    public Task<IReadOnlyList<EventMember>> GetByEventAsync(long eventId, CancellationToken ct)
    {
        return _db.EventMembers
            .Where(x => x.EventId == eventId)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<EventMember>>(t => t.Result, ct);
    }
}
