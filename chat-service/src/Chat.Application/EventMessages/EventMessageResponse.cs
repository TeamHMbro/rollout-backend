namespace Chat.Application.EventMessages;

public sealed record EventMessageResponse(
    long Id,
    long EventId,
    Guid UserId,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    bool IsDeleted);
