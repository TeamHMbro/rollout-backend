using System.Net.Http.Json;
using Chat.Application.Abstractions;
using Chat.Domain;

namespace Chat.Infrastructure.Events;

public sealed class EventAccessService : IEventAccessService
{
    private readonly HttpClient _httpClient;

    public EventAccessService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task EnsureUserCanWriteAsync(long eventId, Guid userId, string bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/events/{eventId}/access");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await _httpClient.SendAsync(request, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new DomainException("Event.NotFound");

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new DomainException("Chat.Unauthorized");

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new DomainException("Chat.AccessDenied");

        if (!response.IsSuccessStatusCode)
            throw new DomainException("Chat.AccessCheckFailed");

        var payload = await response.Content.ReadFromJsonAsync<EventAccessDto>(cancellationToken: ct);

        if (payload == null)
            throw new DomainException("Chat.AccessCheckFailed");

        if (!Guid.TryParse(payload.UserId, out var responseUserId))
            throw new DomainException("Chat.AccessCheckFailed");

        if (responseUserId != userId)
            throw new DomainException("Chat.AccessCheckUserMismatch");

        if (!payload.CanWriteChat)
            throw new DomainException("Chat.AccessDenied");
    }

    private sealed record EventAccessDto(long EventId, string UserId, bool IsOwner, bool IsGoing, bool CanWriteChat);
}
