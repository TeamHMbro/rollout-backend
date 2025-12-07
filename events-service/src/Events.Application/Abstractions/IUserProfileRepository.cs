using Events.Domain.Users;

namespace Events.Application.Abstractions;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(UserProfile profile, CancellationToken ct);
    Task UpdateAsync(UserProfile profile, CancellationToken ct);
}
