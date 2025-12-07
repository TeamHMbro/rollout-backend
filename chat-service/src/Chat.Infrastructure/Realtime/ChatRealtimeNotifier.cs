using Chat.Api;
using Chat.Application.Abstractions;
using Chat.Application.EventMessages;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Infrastructure.Realtime;

public sealed class ChatRealtimeNotifier : IChatRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatRealtimeNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task MessageCreatedAsync(EventMessageResponse message, CancellationToken ct)
    {
        await _hubContext.Clients.Group(EventGroup(message.EventId)).SendAsync("MessageCreated", message, ct);
    }

    public async Task MessageUpdatedAsync(EventMessageResponse message, CancellationToken ct)
    {
        await _hubContext.Clients.Group(EventGroup(message.EventId)).SendAsync("MessageUpdated", message, ct);
    }

    public async Task MessageDeletedAsync(long messageId, long eventId, CancellationToken ct)
    {
        await _hubContext.Clients.Group(EventGroup(eventId)).SendAsync("MessageDeleted", new { messageId, eventId }, ct);
    }

    private static string EventGroup(long eventId) => $"event-{eventId}";
}
