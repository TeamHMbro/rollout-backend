using Events.Domain.Events;

namespace Events.Application.Abstractions;

public interface IEventRepository
{
    Task<EventEntity?> GetByIdAsync(long id, CancellationToken ct);
    Task AddAsync(EventEntity entity, CancellationToken ct);
    Task UpdateAsync(EventEntity entity, CancellationToken ct);

    Task<IReadOnlyList<EventEntity>> GetFeedAsync(
        string? city,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<IReadOnlyList<EventEntity>> GetByOwnerAsync(Guid ownerId, CancellationToken ct);

    Task<IReadOnlyList<EventEntity>> GetGoingByUserAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<EventEntity>> GetSavedByUserAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<EventEntity>> GetLikedByUserAsync(Guid userId, CancellationToken ct);

    Task<bool> TryIncrementMembersAsync(long eventId, DateTimeOffset now, CancellationToken ct);
    Task DecrementMembersAsync(long eventId, DateTimeOffset now, CancellationToken ct);

}