using Events.Application.Abstractions;
using Events.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.Repositories;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly EventDbContext _db;

    public UserProfileRepository(EventDbContext db)
    {
        _db = db;
    }

    public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task AddAsync(UserProfile profile, CancellationToken ct)
    {
        _db.Users.Add(profile);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserProfile profile, CancellationToken ct)
    {
        _db.Users.Update(profile);
        await _db.SaveChangesAsync(ct);
    }
}
