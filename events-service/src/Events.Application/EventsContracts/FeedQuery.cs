namespace Events.Application.EventsContracts;

public sealed record FeedQuery(
    string? City,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize);
