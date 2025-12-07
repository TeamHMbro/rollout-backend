using Auth.Domain.Users;

namespace Auth.Application.Abstractions;

public interface IUserRepository
{
    Task<AuthUser?> GetByEmailAsync(string email, CancellationToken ct);
    Task<AuthUser?> GetByPhoneAsync(string phone, CancellationToken ct);
    Task<AuthUser?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(AuthUser user, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<bool> PhoneExistsAsync(string phone, CancellationToken ct);
}
