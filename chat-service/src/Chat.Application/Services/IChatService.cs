using Chat.Application.EventMessages;

namespace Chat.Application.Services;

public interface IChatService
{
    Task<EventMessageResponse> SendEventMessageAsync(long eventId, Guid userId, SendEventMessageRequest request, CancellationToken ct);
    Task<IReadOnlyList<EventMessageResponse>> GetEventMessagesAsync(long eventId, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<EventMessageResponse>> GetRecentEventMessagesAsync(long eventId, int limit, CancellationToken ct);
    Task<EventMessageResponse> UpdateEventMessageAsync(long messageId, Guid userId, string content, CancellationToken ct);
    Task DeleteEventMessageAsync(long messageId, Guid userId, CancellationToken ct);
}
