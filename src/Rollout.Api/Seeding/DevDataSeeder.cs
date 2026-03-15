using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rollout.Modules.Auth.Data;
using Rollout.Modules.Auth.Entities;
using Rollout.Modules.Auth.Services;
using Rollout.Modules.Events.Data;
using Rollout.Modules.Events.Entities;
using Rollout.Modules.Users.Data;
using Rollout.Modules.Users.Entities;

namespace Rollout.Api.Seeding;

public sealed class DevDataSeeder
{
    private static readonly Guid OwnerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MemberUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid GuestUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly AuthDbContext _authDbContext;
    private readonly UsersDbContext _usersDbContext;
    private readonly EventsDbContext _eventsDbContext;
    private readonly PasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;
    private readonly DevSeedOptions _options;
    private readonly ILogger<DevDataSeeder> _logger;

    public DevDataSeeder(
        AuthDbContext authDbContext,
        UsersDbContext usersDbContext,
        EventsDbContext eventsDbContext,
        PasswordHasher passwordHasher,
        TimeProvider timeProvider,
        IOptions<DevSeedOptions> options,
        ILogger<DevDataSeeder> logger)
    {
        _authDbContext = authDbContext;
        _usersDbContext = usersDbContext;
        _eventsDbContext = eventsDbContext;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        await EnsureUsersAsync(now, cancellationToken);
        await EnsureProfilesAsync(now, cancellationToken);
        await EnsureEventsAsync(now, cancellationToken);

        _logger.LogInformation("Development seed completed.");
    }

    private async Task EnsureUsersAsync(DateTime now, CancellationToken cancellationToken)
    {
        await EnsureUserAsync(OwnerUserId, "owner@example.com", now, cancellationToken);
        await EnsureUserAsync(MemberUserId, "member@example.com", now, cancellationToken);
        await EnsureUserAsync(GuestUserId, "guest@example.com", now, cancellationToken);

        await _authDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUserAsync(Guid userId, string email, DateTime now, CancellationToken cancellationToken)
    {
        var existing = await _authDbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var passwordData = _passwordHasher.Hash(_options.DefaultPassword);

        _authDbContext.Users.Add(new User
        {
            Id = userId,
            Email = email,
            PasswordHash = passwordData.Hash,
            PasswordSalt = passwordData.Salt,
            IsActive = true,
            CreatedAtUtc = now
        });
    }

    private async Task EnsureProfilesAsync(DateTime now, CancellationToken cancellationToken)
    {
        await EnsureProfileAsync(
            OwnerUserId,
            "owner_user",
            "Owner User",
            "Almaty",
            "Creates and manages events.",
            "https://example.com/owner.png",
            now,
            cancellationToken);

        await EnsureProfileAsync(
            MemberUserId,
            "member_user",
            "Member User",
            "Almaty",
            "Regular participant.",
            "https://example.com/member.png",
            now,
            cancellationToken);

        await EnsureProfileAsync(
            GuestUserId,
            "guest_user",
            "Guest User",
            "Astana",
            "Sometimes joins events.",
            "https://example.com/guest.png",
            now,
            cancellationToken);

        await _usersDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureProfileAsync(
        Guid userId,
        string username,
        string displayName,
        string city,
        string bio,
        string avatarUrl,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await _usersDbContext.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        _usersDbContext.UserProfiles.Add(new UserProfile
        {
            UserId = userId,
            Username = username,
            DisplayName = displayName,
            City = city,
            Bio = bio,
            AvatarUrl = avatarUrl,
            CreatedAtUtc = now
        });
    }

    private async Task EnsureEventsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var upcomingEventId = await EnsureEventAsync(
            OwnerUserId,
            "Board Games Night",
            "Play board games together in the city center.",
            "Almaty",
            "Anticafe",
            "Abylai Khan 10",
            "games",
            now.AddDays(2),
            now.AddDays(2).AddHours(2),
            6,
            EventStatus.Active,
            null,
            now,
            cancellationToken);

        var pastEventId = await EnsureEventAsync(
            MemberUserId,
            "Sunday Coffee Meetup",
            "Casual meetup for coffee and networking.",
            "Almaty",
            "Coffee Point",
            "Dostyk 44",
            "networking",
            now.AddDays(-5),
            now.AddDays(-5).AddHours(2),
            8,
            EventStatus.Active,
            null,
            now.AddDays(-7),
            cancellationToken);

        var cancelledEventId = await EnsureEventAsync(
            OwnerUserId,
            "Movie Night",
            "Watching a classic movie together.",
            "Almaty",
            "Cinema Hall",
            "Satpayev 12",
            "movies",
            now.AddDays(4),
            now.AddDays(4).AddHours(3),
            5,
            EventStatus.Cancelled,
            now.AddDays(-1),
            now.AddDays(-8),
            cancellationToken);

        await EnsureMembershipAsync(upcomingEventId, OwnerUserId, now.AddMinutes(-30), cancellationToken);
        await EnsureMembershipAsync(upcomingEventId, MemberUserId, now.AddMinutes(-20), cancellationToken);
        await EnsureMembershipAsync(upcomingEventId, GuestUserId, now.AddMinutes(-10), cancellationToken);

        await EnsureMembershipAsync(pastEventId, MemberUserId, now.AddDays(-10), cancellationToken);
        await EnsureMembershipAsync(pastEventId, OwnerUserId, now.AddDays(-9), cancellationToken);

        await EnsureMembershipAsync(cancelledEventId, OwnerUserId, now.AddDays(-6), cancellationToken);
        await EnsureMembershipAsync(cancelledEventId, MemberUserId, now.AddDays(-5), cancellationToken);

        await _eventsDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<long> EnsureEventAsync(
        Guid creatorUserId,
        string title,
        string description,
        string city,
        string? placeName,
        string? address,
        string? category,
        DateTime startAtUtc,
        DateTime endAtUtc,
        int maxMembers,
        EventStatus status,
        DateTime? cancelledAtUtc,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        var existing = await _eventsDbContext.Events
            .SingleOrDefaultAsync(
                x => x.CreatorUserId == creatorUserId &&
                     x.Title == title &&
                     x.StartAtUtc == startAtUtc,
                cancellationToken);

        if (existing is not null)
        {
            return existing.Id;
        }

        var entity = new Event
        {
            CreatorUserId = creatorUserId,
            Title = title,
            Description = description,
            City = city,
            PlaceName = placeName,
            Address = address,
            Category = category,
            StartAtUtc = startAtUtc,
            EndAtUtc = endAtUtc,
            MaxMembers = maxMembers,
            Status = status,
            CreatedAtUtc = createdAtUtc,
            CancelledAtUtc = cancelledAtUtc
        };

        _eventsDbContext.Events.Add(entity);
        await _eventsDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    private async Task EnsureMembershipAsync(long eventId, Guid userId, DateTime joinedAtUtc, CancellationToken cancellationToken)
    {
        var exists = await _eventsDbContext.EventMembers
            .AnyAsync(x => x.EventId == eventId && x.UserId == userId, cancellationToken);

        if (exists)
        {
            return;
        }

        _eventsDbContext.EventMembers.Add(new EventMember
        {
            EventId = eventId,
            UserId = userId,
            JoinedAtUtc = joinedAtUtc
        });
    }
}