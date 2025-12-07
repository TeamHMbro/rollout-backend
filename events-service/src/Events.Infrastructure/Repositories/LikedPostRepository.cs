using Events.Application.Abstractions;
using Events.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.Repositories;

public sealed class LikedPostRepository : ILikedPostRepository
{
    private readonly EventDbContext _db;

    public LikedPostRepository(EventDbContext db)
    {
        _db = db;
    }

    public Task<LikedPost?> GetAsync(Guid userId, long eventId, CancellationToken ct)
    {
        return _db.LikedPosts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.EventId == eventId, ct);
    }

    public async Task AddAsync(LikedPost like, CancellationToken ct)
    {
        _db.LikedPosts.Add(like);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(LikedPost like, CancellationToken ct)
    {
        _db.LikedPosts.Remove(like);
        await _db.SaveChangesAsync(ct);
    }
}
