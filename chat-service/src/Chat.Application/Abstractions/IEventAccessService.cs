namespace Chat.Application.Abstractions;

public interface IEventAccessService
{
    Task EnsureUserCanWriteAsync(long eventId, Guid userId, string bearerToken, CancellationToken ct);
}
