using Auth.Domain.Tokens;

namespace Auth.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task<RefreshToken?> GetAsync(string token, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
