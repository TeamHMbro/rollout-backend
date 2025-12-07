using Chat.Application.EventMessages;

namespace Chat.Application.Abstractions;

public interface IChatRealtimeNotifier
{
    Task MessageCreatedAsync(EventMessageResponse message, CancellationToken ct);
    Task MessageUpdatedAsync(EventMessageResponse message, CancellationToken ct);
    Task MessageDeletedAsync(long messageId, long eventId, CancellationToken ct);
}
