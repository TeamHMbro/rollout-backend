using Events.Domain.Events;

namespace Events.Application.Abstractions;

public interface ISavedPostRepository
{
    Task<SavedPost?> GetAsync(Guid userId, long eventId, CancellationToken ct);
    Task AddAsync(SavedPost saved, CancellationToken ct);
    Task RemoveAsync(SavedPost saved, CancellationToken ct);
}
