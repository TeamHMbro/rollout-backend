using Chat.Domain.EventMessages;

namespace Chat.Application.Abstractions;

public interface IEventMessageRepository
{
    Task AddAsync(EventMessage message, CancellationToken ct);
    Task<IReadOnlyList<EventMessage>> GetByEventAsync(long eventId, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<EventMessage>> GetRecentByEventAsync(long eventId, int limit, CancellationToken ct);
    Task<EventMessage?> GetByIdAsync(long id, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
