using Events.Application.Abstractions;
using Events.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.Repositories;

public sealed class SavedPostRepository : ISavedPostRepository
{
    private readonly EventDbContext _db;

    public SavedPostRepository(EventDbContext db)
    {
        _db = db;
    }

    public Task<SavedPost?> GetAsync(Guid userId, long eventId, CancellationToken ct)
    {
        return _db.SavedPosts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.EventId == eventId, ct);
    }

    public async Task AddAsync(SavedPost saved, CancellationToken ct)
    {
        _db.SavedPosts.Add(saved);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(SavedPost saved, CancellationToken ct)
    {
        _db.SavedPosts.Remove(saved);
        await _db.SaveChangesAsync(ct);
    }
}
