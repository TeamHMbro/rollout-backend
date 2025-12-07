using Auth.Application.Abstractions;
using Auth.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;

    public UserRepository(AuthDbContext db)
    {
        _db = db;
    }

    public Task<AuthUser?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return _db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);
    }

    public Task<AuthUser?> GetByPhoneAsync(string phone, CancellationToken ct)
    {
        return _db.Users.FirstOrDefaultAsync(x => x.Phone == phone, ct);
    }

    public Task<AuthUser?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task AddAsync(AuthUser user, CancellationToken ct)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        return _db.Users.AnyAsync(x => x.Email == email, ct);
    }

    public Task<bool> PhoneExistsAsync(string phone, CancellationToken ct)
    {
        return _db.Users.AnyAsync(x => x.Phone == phone, ct);
    }
}
