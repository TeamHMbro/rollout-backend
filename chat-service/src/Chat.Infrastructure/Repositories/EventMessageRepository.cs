using Chat.Application.Abstractions;
using Chat.Domain.EventMessages;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Repositories;

public sealed class EventMessageRepository : IEventMessageRepository
{
    private readonly ChatDbContext _db;

    public EventMessageRepository(ChatDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(EventMessage message, CancellationToken ct)
    {
        _db.EventMessages.Add(message);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EventMessage>> GetByEventAsync(long eventId, int page, int pageSize, CancellationToken ct)
    {
        var skip = (page - 1) * pageSize;

        return await _db.EventMessages
            .Where(x => x.EventId == eventId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EventMessage>> GetRecentByEventAsync(long eventId, int limit, CancellationToken ct)
    {
        return await _db.EventMessages
            .Where(x => x.EventId == eventId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<EventMessage?> GetByIdAsync(long id, CancellationToken ct)
    {
        return await _db.EventMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);
    }
}
