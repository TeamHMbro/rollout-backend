using Events.Domain.Events;

namespace Events.Application.Abstractions;

public interface ILikedPostRepository
{
    Task<LikedPost?> GetAsync(Guid userId, long eventId, CancellationToken ct);
    Task AddAsync(LikedPost like, CancellationToken ct);
    Task RemoveAsync(LikedPost like, CancellationToken ct);
}
