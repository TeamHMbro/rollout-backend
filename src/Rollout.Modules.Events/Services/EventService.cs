using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rollout.Modules.Events.Data;
using Rollout.Modules.Events.Dtos;
using Rollout.Modules.Events.Entities;
using Rollout.Modules.Users.Data;
using Rollout.Modules.Users.Entities;
using Rollout.Shared.Exceptions;

namespace Rollout.Modules.Events.Services;

public sealed class EventService
{
    private readonly EventsDbContext _eventsDbContext;
    private readonly UsersDbContext _usersDbContext;
    private readonly TimeProvider _timeProvider;

    public EventService(EventsDbContext eventsDbContext, UsersDbContext usersDbContext, TimeProvider timeProvider)
    {
        _eventsDbContext = eventsDbContext;
        _usersDbContext = usersDbContext;
        _timeProvider = timeProvider;
    }

    public async Task<EventDetailsResponse> CreateAsync(Guid currentUserId, CreateEventRequest request, CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var startAtUtc = NormalizeUtc(request.StartAtUtc);
        var endAtUtc = NormalizeUtc(request.EndAtUtc);

        var entity = new Event
        {
            CreatorUserId = currentUserId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            City = request.City.Trim(),
            PlaceName = NormalizeOptional(request.PlaceName),
            Address = NormalizeOptional(request.Address),
            Category = NormalizeOptional(request.Category),
            StartAtUtc = startAtUtc,
            EndAtUtc = endAtUtc,
            MaxMembers = request.MaxMembers,
            Status = EventStatus.Active,
            CreatedAtUtc = now
        };

        _eventsDbContext.Events.Add(entity);
        await _eventsDbContext.SaveChangesAsync(cancellationToken);

        _eventsDbContext.EventMembers.Add(new EventMember
        {
            EventId = entity.Id,
            UserId = currentUserId,
            JoinedAtUtc = now
        });

        await _eventsDbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, currentUserId, cancellationToken)
               ?? throw new AppException(StatusCodes.Status500InternalServerError, "event_create_failed", "Event was created but could not be loaded.");
    }

    public async Task<EventDetailsResponse?> GetByIdAsync(long eventId, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var entity = await _eventsDbContext.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var membersCount = await _eventsDbContext.EventMembers
            .CountAsync(x => x.EventId == eventId, cancellationToken);

        var isJoined = currentUserId.HasValue && await _eventsDbContext.EventMembers
            .AnyAsync(x => x.EventId == eventId && x.UserId == currentUserId.Value, cancellationToken);

        var creator = await GetUserSummaryAsync(entity.CreatorUserId, cancellationToken);

        return new EventDetailsResponse
        {
            EventId = entity.Id,
            CreatorUserId = entity.CreatorUserId,
            Title = entity.Title,
            Description = entity.Description,
            City = entity.City,
            PlaceName = entity.PlaceName,
            Address = entity.Address,
            Category = entity.Category,
            StartAtUtc = entity.StartAtUtc,
            EndAtUtc = entity.EndAtUtc,
            MembersCount = membersCount,
            MaxMembers = entity.MaxMembers,
            Status = MapStatus(entity.Status),
            IsJoined = isJoined,
            IsCreator = currentUserId == entity.CreatorUserId,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            CancelledAtUtc = entity.CancelledAtUtc,
            Creator = creator
        };
    }

    public async Task<PagedResponse<EventListItemResponse>> GetFeedAsync(
        string? q,
        string? city,
        string? category,
        DateTime? from,
        DateTime? to,
        bool? onlyAvailable,
        string? sort,
        int page,
        int pageSize,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);

        var query = _eventsDbContext.Events
            .AsNoTracking()
            .Where(x => x.Status == EventStatus.Active);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalizedQuery = q.Trim().ToLowerInvariant();

            query = query.Where(x =>
                x.Title.ToLower().Contains(normalizedQuery) ||
                x.Description.ToLower().Contains(normalizedQuery) ||
                (x.PlaceName != null && x.PlaceName.ToLower().Contains(normalizedQuery)) ||
                (x.Address != null && x.Address.ToLower().Contains(normalizedQuery)));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalizedCity = city.Trim().ToLowerInvariant();
            query = query.Where(x => x.City.ToLower() == normalizedCity);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();
            query = query.Where(x => x.Category != null && x.Category.ToLower() == normalizedCategory);
        }

        if (from.HasValue)
        {
            var fromUtc = NormalizeUtc(from.Value);
            query = query.Where(x => x.EndAtUtc >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = NormalizeUtc(to.Value);
            query = query.Where(x => x.StartAtUtc <= toUtc);
        }

        if (onlyAvailable == true)
        {
            query = query.Where(x => _eventsDbContext.EventMembers.Count(m => m.EventId == x.Id) < x.MaxMembers);
        }

        query = ApplyFeedSorting(query, sort);

        var totalCount = await query.CountAsync(cancellationToken);

        var events = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = await MapEventListAsync(events, currentUserId, cancellationToken);

        return new PagedResponse<EventListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<EventDetailsResponse> UpdateAsync(long eventId, Guid currentUserId, UpdateEventRequest request, CancellationToken cancellationToken)
    {
        var entity = await _eventsDbContext.Events
            .SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);

        if (entity is null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "event_not_found", "Event was not found.");
        }

        EnsureCreator(entity, currentUserId);
        EnsureActive(entity);

        if (request.Title is not null)
        {
            ValidateTitle(request.Title);
            entity.Title = request.Title.Trim();
        }

        if (request.Description is not null)
        {
            ValidateDescription(request.Description);
            entity.Description = request.Description.Trim();
        }

        if (request.City is not null)
        {
            ValidateCity(request.City);
            entity.City = request.City.Trim();
        }

        if (request.PlaceName is not null)
        {
            ValidatePlaceName(request.PlaceName);
            entity.PlaceName = NormalizeOptional(request.PlaceName);
        }

        if (request.Address is not null)
        {
            ValidateAddress(request.Address);
            entity.Address = NormalizeOptional(request.Address);
        }

        if (request.Category is not null)
        {
            ValidateCategory(request.Category);
            entity.Category = NormalizeOptional(request.Category);
        }

        if (request.StartAtUtc.HasValue)
        {
            entity.StartAtUtc = NormalizeUtc(request.StartAtUtc.Value);
        }

        if (request.EndAtUtc.HasValue)
        {
            entity.EndAtUtc = NormalizeUtc(request.EndAtUtc.Value);
        }

        if (request.MaxMembers.HasValue)
        {
            ValidateMaxMembers(request.MaxMembers.Value);

            var membersCount = await _eventsDbContext.EventMembers
                .CountAsync(x => x.EventId == eventId, cancellationToken);

            if (request.MaxMembers.Value < membersCount)
            {
                throw new AppException(StatusCodes.Status400BadRequest, "invalid_max_members", "Max members cannot be less than current members count.");
            }

            entity.MaxMembers = request.MaxMembers.Value;
        }

        ValidateEventTimeRange(entity.StartAtUtc, entity.EndAtUtc);

        entity.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _eventsDbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(eventId, currentUserId, cancellationToken)
               ?? throw new AppException(StatusCodes.Status500InternalServerError, "event_update_failed", "Updated event could not be loaded.");
    }

    public async Task CancelAsync(long eventId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var entity = await _eventsDbContext.Events
            .SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);

        if (entity is null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "event_not_found", "Event was not found.");
        }

        EnsureCreator(entity, currentUserId);
        EnsureActive(entity);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        entity.Status = EventStatus.Cancelled;
        entity.CancelledAtUtc = now;
        entity.UpdatedAtUtc = now;

        await _eventsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task JoinAsync(long eventId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var entity = await _eventsDbContext.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);

        if (entity is null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "event_not_found", "Event was not found.");
        }

        EnsureActive(entity);

        var alreadyJoined = await _eventsDbContext.EventMembers
            .AnyAsync(x => x.EventId == eventId && x.UserId == currentUserId, cancellationToken);

        if (alreadyJoined)
        {
            return;
        }

        var membersCount = await _eventsDbContext.EventMembers
            .CountAsync(x => x.EventId == eventId, cancellationToken);

        if (membersCount >= entity.MaxMembers)
        {
            throw new AppException(StatusCodes.Status409Conflict, "event_full", "Event has reached max members.");
        }

        _eventsDbContext.EventMembers.Add(new EventMember
        {
            EventId = eventId,
            UserId = currentUserId,
            JoinedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        });

        await _eventsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task LeaveAsync(long eventId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var entity = await _eventsDbContext.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);

        if (entity is null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "event_not_found", "Event was not found.");
        }

        if (entity.CreatorUserId == currentUserId)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "creator_cannot_leave", "Event creator cannot leave their own event.");
        }

        var membership = await _eventsDbContext.EventMembers
            .SingleOrDefaultAsync(x => x.EventId == eventId && x.UserId == currentUserId, cancellationToken);

        if (membership is null)
        {
            return;
        }

        _eventsDbContext.EventMembers.Remove(membership);
        await _eventsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EventMemberResponse>> GetMembersAsync(long eventId, CancellationToken cancellationToken)
    {
        var eventExists = await _eventsDbContext.Events
            .AsNoTracking()
            .AnyAsync(x => x.Id == eventId, cancellationToken);

        if (!eventExists)
        {
            throw new AppException(StatusCodes.Status404NotFound, "event_not_found", "Event was not found.");
        }

        var memberships = await _eventsDbContext.EventMembers
            .AsNoTracking()
            .Where(x => x.EventId == eventId)
            .OrderBy(x => x.JoinedAtUtc)
            .ToListAsync(cancellationToken);

        var profiles = await GetUserSummariesAsync(memberships.Select(x => x.UserId).Distinct(), cancellationToken);

        return memberships
            .Select(x =>
            {
                var profile = profiles[x.UserId];

                return new EventMemberResponse
                {
                    UserId = x.UserId,
                    Username = profile.Username,
                    DisplayName = profile.DisplayName,
                    AvatarUrl = profile.AvatarUrl,
                    JoinedAtUtc = x.JoinedAtUtc
                };
            })
            .ToList();
    }

    public async Task<PagedResponse<EventListItemResponse>> GetMyCreatedAsync(Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);

        var query = _eventsDbContext.Events
            .AsNoTracking()
            .Where(x => x.CreatorUserId == currentUserId)
            .OrderByDescending(x => x.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var events = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = await MapEventListAsync(events, currentUserId, cancellationToken);

        return new PagedResponse<EventListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResponse<EventListItemResponse>> GetMyJoinedAsync(Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);

        var joinedEventIdsQuery = _eventsDbContext.EventMembers
            .Where(x => x.UserId == currentUserId)
            .Select(x => x.EventId);

        var query = _eventsDbContext.Events
            .AsNoTracking()
            .Where(x => joinedEventIdsQuery.Contains(x.Id))
            .OrderBy(x => x.StartAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var events = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = await MapEventListAsync(events, currentUserId, cancellationToken);

        return new PagedResponse<EventListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResponse<EventListItemResponse>> GetMyCalendarAsync(
        Guid currentUserId,
        string status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);

        var normalizedStatus = status.Trim().ToLowerInvariant();
        if (normalizedStatus is not ("upcoming" or "past"))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_calendar_status", "Calendar status must be either upcoming or past.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var joinedEventIdsQuery = _eventsDbContext.EventMembers
            .Where(x => x.UserId == currentUserId)
            .Select(x => x.EventId);

        var query = _eventsDbContext.Events
            .AsNoTracking()
            .Where(x => joinedEventIdsQuery.Contains(x.Id))
            .Where(x => x.Status == EventStatus.Active);

        query = normalizedStatus == "upcoming"
            ? query.Where(x => x.EndAtUtc >= now).OrderBy(x => x.StartAtUtc)
            : query.Where(x => x.EndAtUtc < now).OrderByDescending(x => x.StartAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var events = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = await MapEventListAsync(events, currentUserId, cancellationToken);

        return new PagedResponse<EventListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private async Task<List<EventListItemResponse>> MapEventListAsync(
        IReadOnlyCollection<Event> events,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return new List<EventListItemResponse>();
        }

        var eventIds = events.Select(x => x.Id).ToArray();
        var creatorIds = events.Select(x => x.CreatorUserId).Distinct().ToArray();

        var membersCountLookup = await _eventsDbContext.EventMembers
            .AsNoTracking()
            .Where(x => eventIds.Contains(x.EventId))
            .GroupBy(x => x.EventId)
            .Select(x => new { EventId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count, cancellationToken);

        HashSet<long> joinedEventIds = new();
        if (currentUserId.HasValue)
        {
            joinedEventIds = await _eventsDbContext.EventMembers
                .AsNoTracking()
                .Where(x => x.UserId == currentUserId.Value && eventIds.Contains(x.EventId))
                .Select(x => x.EventId)
                .ToHashSetAsync(cancellationToken);
        }

        var creatorsLookup = await GetUserSummariesAsync(creatorIds, cancellationToken);

        return events
            .Select(x =>
            {
                var membersCount = membersCountLookup.GetValueOrDefault(x.Id);
                var availableSpots = Math.Max(x.MaxMembers - membersCount, 0);

                return new EventListItemResponse
                {
                    EventId = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    City = x.City,
                    PlaceName = x.PlaceName,
                    Category = x.Category,
                    StartAtUtc = x.StartAtUtc,
                    EndAtUtc = x.EndAtUtc,
                    MembersCount = membersCount,
                    MaxMembers = x.MaxMembers,
                    AvailableSpots = availableSpots,
                    IsFull = availableSpots == 0,
                    Status = MapStatus(x.Status),
                    IsJoined = joinedEventIds.Contains(x.Id),
                    IsCreator = currentUserId == x.CreatorUserId,
                    Creator = creatorsLookup[x.CreatorUserId]
                };
            })
            .ToList();
    }

    private async Task<EventUserSummaryResponse> GetUserSummaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profiles = await GetUserSummariesAsync(new[] { userId }, cancellationToken);
        return profiles[userId];
    }

    private async Task<Dictionary<Guid, EventUserSummaryResponse>> GetUserSummariesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();

        var profiles = await _usersDbContext.UserProfiles
            .AsNoTracking()
            .Where(x => ids.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var lookup = profiles.ToDictionary(
            x => x.UserId,
            x => new EventUserSummaryResponse
            {
                UserId = x.UserId,
                Username = x.Username,
                DisplayName = x.DisplayName,
                AvatarUrl = x.AvatarUrl
            });

        foreach (var userId in ids)
        {
            if (!lookup.ContainsKey(userId))
            {
                lookup[userId] = BuildFallbackUserSummary(userId);
            }
        }

        return lookup;
    }

    private static EventUserSummaryResponse BuildFallbackUserSummary(Guid userId)
    {
        var value = $"user{userId:N}"[..12];

        return new EventUserSummaryResponse
        {
            UserId = userId,
            Username = value.ToLowerInvariant(),
            DisplayName = value,
            AvatarUrl = null
        };
    }

    private static void EnsureCreator(Event entity, Guid currentUserId)
    {
        if (entity.CreatorUserId != currentUserId)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "forbidden", "Only event creator can perform this action.");
        }
    }

    private static void EnsureActive(Event entity)
    {
        if (entity.Status != EventStatus.Active)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "event_inactive", "Event is not active.");
        }
    }

    private static void ValidateCreateRequest(CreateEventRequest request)
    {
        ValidateTitle(request.Title);
        ValidateDescription(request.Description);
        ValidateCity(request.City);
        ValidatePlaceName(request.PlaceName);
        ValidateAddress(request.Address);
        ValidateCategory(request.Category);
        ValidateMaxMembers(request.MaxMembers);

        var startAtUtc = NormalizeUtc(request.StartAtUtc);
        var endAtUtc = NormalizeUtc(request.EndAtUtc);

        ValidateEventTimeRange(startAtUtc, endAtUtc);
    }

    private static void ValidateTitle(string title)
    {
        var value = title.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 200)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_title", "Title is invalid.");
        }
    }

    private static void ValidateDescription(string description)
    {
        var value = description.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2000)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_description", "Description is invalid.");
        }
    }

    private static void ValidateCity(string city)
    {
        var value = city.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_city", "City is invalid.");
        }
    }

    private static void ValidatePlaceName(string? placeName)
    {
        if (!string.IsNullOrWhiteSpace(placeName) && placeName.Trim().Length > 200)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_place_name", "Place name is too long.");
        }
    }

    private static void ValidateAddress(string? address)
    {
        if (!string.IsNullOrWhiteSpace(address) && address.Trim().Length > 300)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_address", "Address is too long.");
        }
    }

    private static void ValidateCategory(string? category)
    {
        if (!string.IsNullOrWhiteSpace(category) && category.Trim().Length > 100)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_category", "Category is too long.");
        }
    }

    private static void ValidateMaxMembers(int maxMembers)
    {
        if (maxMembers < 1 || maxMembers > 100000)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_max_members", "Max members must be between 1 and 100000.");
        }
    }

    private static void ValidateEventTimeRange(DateTime startAtUtc, DateTime endAtUtc)
    {
        if (startAtUtc >= endAtUtc)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_time_range", "Event start time must be earlier than end time.");
        }
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int NormalizePage(int page)
    {
        return page <= 0 ? 1 : page;
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return 20;
        }

        return Math.Min(pageSize, 100);
    }

    private static string MapStatus(EventStatus status)
    {
        return status switch
        {
            EventStatus.Active => "active",
            EventStatus.Cancelled => "cancelled",
            _ => "unknown"
        };
    }

    private IQueryable<Event> ApplyFeedSorting(IQueryable<Event> query, string? sort)
    {
        var normalizedSort = string.IsNullOrWhiteSpace(sort)
            ? "soon"
            : sort.Trim().ToLowerInvariant();

        return normalizedSort switch
        {
            "soon" => query.OrderBy(x => x.StartAtUtc).ThenByDescending(x => x.CreatedAtUtc),
            "new" => query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.StartAtUtc),
            "popular" => query
                .OrderByDescending(x => _eventsDbContext.EventMembers.Count(m => m.EventId == x.Id))
                .ThenBy(x => x.StartAtUtc),
            _ => throw new AppException(StatusCodes.Status400BadRequest, "invalid_feed_sort", "Feed sort must be soon, new, or popular.")
        };
    }
}