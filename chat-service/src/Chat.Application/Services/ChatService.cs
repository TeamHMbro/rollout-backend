using Chat.Application.Abstractions;
using Chat.Application.EventMessages;
using Chat.Domain;
using Chat.Domain.EventMessages;

namespace Chat.Application.Services;

public sealed class ChatService : IChatService
{
    private readonly IEventMessageRepository _messages;
    private readonly IDateTimeProvider _clock;
    private readonly IChatRealtimeNotifier _notifier;

    public ChatService(IEventMessageRepository messages, IDateTimeProvider clock, IChatRealtimeNotifier notifier)
    {
        _messages = messages;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<EventMessageResponse> SendEventMessageAsync(long eventId, Guid userId, SendEventMessageRequest request, CancellationToken ct)
    {
        var now = _clock.UtcNow;

        var message = new EventMessage(eventId, userId, request.Content, now);

        await _messages.AddAsync(message, ct);

        var response = Map(message);
        await _notifier.MessageCreatedAsync(response, ct);
        return response;
    }

    public async Task<IReadOnlyList<EventMessageResponse>> GetEventMessagesAsync(long eventId, int page, int pageSize, CancellationToken ct)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 50 : pageSize;
        pageSize = Math.Min(pageSize, 200);

        var list = await _messages.GetByEventAsync(eventId, page, pageSize, ct);

        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<EventMessageResponse>> GetRecentEventMessagesAsync(long eventId, int limit, CancellationToken ct)
    {
        limit = limit <= 0 ? 50 : limit;
        limit = Math.Min(limit, 200);

        var list = await _messages.GetRecentByEventAsync(eventId, limit, ct);

        return list.Select(Map).ToList();
    }

    public async Task<EventMessageResponse> UpdateEventMessageAsync(long messageId, Guid userId, string content, CancellationToken ct)
    {
        var message = await _messages.GetByIdAsync(messageId, ct);
        if (message == null)
            throw new DomainException("Chat.MessageNotFound");
        if (message.UserId != userId)
            throw new DomainException("Chat.Forbidden");

        message.Edit(content, _clock.UtcNow);
        await _messages.SaveChangesAsync(ct);

        var response = Map(message);
        await _notifier.MessageUpdatedAsync(response, ct);
        return response;
    }

    public async Task DeleteEventMessageAsync(long messageId, Guid userId, CancellationToken ct)
    {
        var message = await _messages.GetByIdAsync(messageId, ct);
        if (message == null)
            throw new DomainException("Chat.MessageNotFound");
        if (message.UserId != userId)
            throw new DomainException("Chat.Forbidden");

        message.Delete();
        await _messages.SaveChangesAsync(ct);
        await _notifier.MessageDeletedAsync(messageId, message.EventId, ct);
    }

    private static EventMessageResponse Map(EventMessage m)
    {
        return new EventMessageResponse(m.Id, m.EventId, m.UserId, m.Content, m.CreatedAt, m.EditedAt, m.IsDeleted);
    }
}
