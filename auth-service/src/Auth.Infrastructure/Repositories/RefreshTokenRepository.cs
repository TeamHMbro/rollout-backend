using Auth.Application.Abstractions;
using Auth.Domain.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _db;

    public RefreshTokenRepository(AuthDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(RefreshToken token, CancellationToken ct)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(ct);
    }

    public Task<RefreshToken?> GetAsync(string token, CancellationToken ct)
    {
        return _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }
}
