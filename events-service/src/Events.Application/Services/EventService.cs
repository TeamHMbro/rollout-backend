using Events.Application.Abstractions;
using Events.Application.EventsContracts;
using Events.Domain;
using Events.Domain.Events;
using Events.Domain.Users;

namespace Events.Application.Services;

public interface IEventService
{
      Task<EventResponse> CreateAsync(CreateEventRequest request, Guid userId, CancellationToken ct);
    Task<EventResponse?> GetAsync(long eventId, CancellationToken ct);
    Task<IReadOnlyList<FeedItemResponse>> GetFeedAsync(FeedQuery query, CancellationToken ct);
    Task JoinAsync(long eventId, Guid userId, CancellationToken ct);
    Task LeaveAsync(long eventId, Guid userId, CancellationToken ct);
    Task LikeAsync(long eventId, Guid userId, CancellationToken ct);
    Task UnlikeAsync(long eventId, Guid userId, CancellationToken ct);
    Task SaveAsync(long eventId, Guid userId, CancellationToken ct);
    Task UnsaveAsync(long eventId, Guid userId, CancellationToken ct);

    Task<IReadOnlyList<EventResponse>> GetMyCreatedEventsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<EventResponse>> GetMyGoingEventsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<EventResponse>> GetMySavedEventsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<EventResponse>> GetMyLikedEventsAsync(Guid userId, CancellationToken ct);

    Task<IReadOnlyList<EventMemberResponse>> GetMembersAsync(long eventId, CancellationToken ct);
    Task UpdateAsync(long eventId, Guid userId, UpdateEventRequest request, CancellationToken ct);
    Task CancelAsync(long eventId, Guid userId, CancellationToken ct);


}

public sealed class EventService : IEventService
{
    private readonly IEventRepository _events;
    private readonly IEventMemberRepository _members;
    private readonly ILikedPostRepository _likes;
    private readonly ISavedPostRepository _saves;
    private readonly IUserProfileRepository _profiles;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public EventService(
        IEventRepository events,
        IEventMemberRepository members,
        ILikedPostRepository likes,
        ISavedPostRepository saves,
        IUserProfileRepository profiles,
        IDateTimeProvider clock,
        IUnitOfWork uow)
    {
        _events = events;
        _members = members;
        _likes = likes;
        _saves = saves;
        _profiles = profiles;
        _clock = clock;
        _uow = uow;
    }


    public async Task<EventResponse> CreateAsync(CreateEventRequest request, Guid userId, CancellationToken ct)
    {
        var now = _clock.UtcNow;

        var entity = new EventEntity(
            userId,
            request.Title,
            request.Description,
            request.Type,
            request.City,
            request.Address,
            request.Visibility,
            request.MaxMembers,
            request.Price,
            request.Payment,
            request.EventStartAt,
            request.EventEndAt,
            request.IsRecurring,
            request.RecurrenceRule,
            request.CallLink,
            now);

        await _events.AddAsync(entity, ct);

        return MapToResponse(entity);
    }

    public async Task<EventResponse?> GetAsync(long eventId, CancellationToken ct)
    {
        var ev = await _events.GetByIdAsync(eventId, ct);
        if (ev is null)
            return null;

        return MapToResponse(ev);
    }

    public async Task<IReadOnlyList<FeedItemResponse>> GetFeedAsync(FeedQuery query, CancellationToken ct)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

        var from = query.From ?? _clock.UtcNow;
        var to = query.To;

        var list = await _events.GetFeedAsync(query.City, from, to, page, pageSize, ct);

        return list
            .Select(e => new FeedItemResponse(
                e.Id,
                e.OwnerId,
                e.Title,
                e.City,
                e.Address,
                e.Type,
                e.Visibility,
                e.EventStartAt,
                e.EventEndAt,
                e.MembersCount,
                e.MaxMembers))
            .ToList();
    }

    public async Task JoinAsync(long eventId, Guid userId, CancellationToken ct)
    {
        await _uow.ExecuteAsync(async token =>
        {
            var ev = await _events.GetByIdAsync(eventId, token);
            if (ev is null)
                throw new DomainException("Event.NotFound");

            var now = _clock.UtcNow;

            if (ev.Status != EventStatus.Published || ev.EventStartAt <= now)
                throw new DomainException("Event.NotActiveForJoin");

            var existing = await _members.GetAsync(eventId, userId, token);
            if (existing is not null && existing.Status == RsvpStatus.Going)
                return;

            var incremented = await _events.TryIncrementMembersAsync(eventId, now, token);
            if (!incremented)
                throw new DomainException("Event.MaxMembersReached");

            if (existing is null)
            {
                var member = new EventMember(eventId, userId, ParticipantRole.Participant, now);
                await _members.AddAsync(member, token);
            }
            else
            {
                existing.Rejoin(now);
                await _members.UpdateAsync(existing, token);
            }
        }, ct);
    }

    public async Task LeaveAsync(long eventId, Guid userId, CancellationToken ct)
    {
        await _uow.ExecuteAsync(async token =>
        {
            var ev = await _events.GetByIdAsync(eventId, token);
            if (ev is null)
                throw new DomainException("Event.NotFound");

            var existing = await _members.GetAsync(eventId, userId, token);
            if (existing is null || existing.Status != RsvpStatus.Going)
                return;

            var now = _clock.UtcNow;

            await _events.DecrementMembersAsync(eventId, now, token);

            existing.Leave();
            await _members.UpdateAsync(existing, token);
        }, ct);
    }

    public async Task LikeAsync(long eventId, Guid userId, CancellationToken ct)
    {
        await _uow.ExecuteAsync(async token =>
        {
            var ev = await _events.GetByIdAsync(eventId, token);
            if (ev is null)
                throw new DomainException("Event.NotFound");

            var existing = await _likes.GetAsync(userId, eventId, token);
            if (existing is not null)
                return;

            var now = _clock.UtcNow;

            ev.IncrementLikes(now);
            await _events.UpdateAsync(ev, token);

            var like = new LikedPost(userId, eventId, now);
            await _likes.AddAsync(like, token);
        }, ct);
    }

    public async Task UnlikeAsync(long eventId, Guid userId, CancellationToken ct)
    {
        await _uow.ExecuteAsync(async token =>
        {
            var ev = await _events.GetByIdAsync(eventId, token);
            if (ev is null)
                throw new DomainException("Event.NotFound");

            var existing = await _likes.GetAsync(userId, eventId, token);
            if (existing is null)
                return;

            var now = _clock.UtcNow;

            ev.DecrementLikes(now);
            await _events.UpdateAsync(ev, token);

            await _likes.RemoveAsync(existing, token);
        }, ct);
    }


    public async Task SaveAsync(long eventId, Guid userId, CancellationToken ct)
    {
        var ev = await _events.GetByIdAsync(eventId, ct);
        if (ev is null)
            throw new DomainException("Event.NotFound");

        var existing = await _saves.GetAsync(userId, eventId, ct);
        if (existing is not null)
            return;

        var now = _clock.UtcNow;

        var saved = new SavedPost(userId, eventId, now);
        await _saves.AddAsync(saved, ct);
    }


    public async Task UnsaveAsync(long eventId, Guid userId, CancellationToken ct)
    {
        var ev = await _events.GetByIdAsync(eventId, ct);
        if (ev is null)
            throw new DomainException("Event.NotFound");

        var existing = await _saves.GetAsync(userId, eventId, ct);
        if (existing is null)
            return;

        await _saves.RemoveAsync(existing, ct);
    }

    public async Task<IReadOnlyList<EventResponse>> GetMyCreatedEventsAsync(Guid userId, CancellationToken ct)
    {
        var list = await _events.GetByOwnerAsync(userId, ct);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<EventResponse>> GetMyGoingEventsAsync(Guid userId, CancellationToken ct)
    {
        var list = await _events.GetGoingByUserAsync(userId, ct);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<EventResponse>> GetMySavedEventsAsync(Guid userId, CancellationToken ct)
    {
        var list = await _events.GetSavedByUserAsync(userId, ct);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<EventResponse>> GetMyLikedEventsAsync(Guid userId, CancellationToken ct)
    {
        var list = await _events.GetLikedByUserAsync(userId, ct);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<EventMemberResponse>> GetMembersAsync(long eventId, CancellationToken ct)
    {
        var members = await _members.GetByEventAsync(eventId, ct);
        if (members.Count == 0)
            return Array.Empty<EventMemberResponse>();

        var cache = new Dictionary<Guid, UserProfile?>();
        var result = new List<EventMemberResponse>(members.Count);

        foreach (var member in members)
        {
            if (!cache.TryGetValue(member.UserId, out var profile))
            {
                profile = await _profiles.GetByIdAsync(member.UserId, ct);
                cache[member.UserId] = profile;
            }

            result.Add(new EventMemberResponse(
                member.UserId,
                profile?.UserName ?? string.Empty,
                profile?.Avatar,
                member.Status,
                member.Role,
                member.JoinedAt));
        }

        return result;
    }

    public async Task UpdateAsync(long eventId, Guid userId, UpdateEventRequest request, CancellationToken ct)
    {
        var ev = await _events.GetByIdAsync(eventId, ct);
        if (ev is null)
            throw new DomainException("Event.NotFound");

        if (ev.OwnerId != userId)
            throw new DomainException("Event.NotOwner");

        var now = _clock.UtcNow;

        ev.Update(
            request.Title,
            request.Description,
            request.City,
            request.Address,
            request.MaxMembers,
            request.Price,
            request.Payment,
            request.EventStartAt,
            request.EventEndAt,
            request.IsRecurring,
            request.RecurrenceRule,
            request.CallLink,
            request.Visibility,
            now);

        await _events.UpdateAsync(ev, ct);
    }

    public async Task CancelAsync(long eventId, Guid userId, CancellationToken ct)
    {
        var ev = await _events.GetByIdAsync(eventId, ct);
        if (ev is null)
            throw new DomainException("Event.NotFound");

        if (ev.OwnerId != userId)
            throw new DomainException("Event.NotOwner");

        var now = _clock.UtcNow;
        ev.Cancel(now);

        await _events.UpdateAsync(ev, ct);
    }

    private static EventResponse MapToResponse(EventEntity e)
    {
        return new EventResponse(
            e.Id,
            e.OwnerId,
            e.Title,
            e.Description,
            e.Type,
            e.City,
            e.Address,
            e.Visibility,
            e.Status,
            e.MaxMembers,
            e.MembersCount,
            e.Price,
            e.Payment,
            e.EventStartAt,
            e.EventEndAt,
            e.IsRecurring,
            e.RecurrenceRule,
            e.CallLink);
    }
}