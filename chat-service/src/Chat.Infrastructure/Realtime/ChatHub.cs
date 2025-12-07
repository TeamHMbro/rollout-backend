using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Api;

[Authorize]
public sealed class ChatHub : Hub
{
    public async Task JoinEvent(long eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(eventId));
    }

    public async Task LeaveEvent(long eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(eventId));
    }

    private static string GroupName(long eventId) => $"event-{eventId}";
}
