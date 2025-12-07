using Events.Domain.Events;

namespace Events.Application.Abstractions;

public interface IEventMemberRepository
{
    Task<EventMember?> GetAsync(long eventId, Guid userId, CancellationToken ct);
    Task AddAsync(EventMember member, CancellationToken ct);
    Task UpdateAsync(EventMember member, CancellationToken ct);
    Task<int> CountByEventAsync(long eventId, CancellationToken ct);

    Task<IReadOnlyList<EventMember>> GetByEventAsync(long eventId, CancellationToken ct);
}
