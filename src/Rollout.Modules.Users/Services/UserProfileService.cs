using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rollout.Modules.Users.Data;
using Rollout.Modules.Users.Dtos;
using Rollout.Modules.Users.Entities;
using Rollout.Shared.Exceptions;

namespace Rollout.Modules.Users.Services;

public sealed class UserProfileService
{
    private readonly UsersDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public UserProfileService(UsersDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<MyProfileResponse> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await GetRequiredProfileAsync(userId, cancellationToken);
        return Map(profile);
    }

    public async Task<MyProfileResponse> UpdateMyProfileAsync(Guid userId, UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await GetRequiredProfileAsync(userId, cancellationToken);

        if (request.Username is not null)
        {
            var username = NormalizeUsername(request.Username);
            ValidateUsername(username);

            var usernameTaken = await _dbContext.UserProfiles
                .AnyAsync(x => x.Username == username && x.UserId != userId, cancellationToken);

            if (usernameTaken)
            {
                throw new AppException(StatusCodes.Status409Conflict, "username_taken", "Username is already taken.");
            }

            profile.Username = username;
        }

        if (request.DisplayName is not null)
        {
            var displayName = request.DisplayName.Trim();
            ValidateDisplayName(displayName);
            profile.DisplayName = displayName;
        }

        if (request.City is not null)
        {
            var city = request.City.Trim();
            ValidateCity(city);
            profile.City = string.IsNullOrWhiteSpace(city) ? null : city;
        }

        if (request.Bio is not null)
        {
            var bio = request.Bio.Trim();
            ValidateBio(bio);
            profile.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio;
        }

        if (request.AvatarUrl is not null)
        {
            var avatarUrl = request.AvatarUrl.Trim();
            ValidateAvatarUrl(avatarUrl);
            profile.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl;
        }

        profile.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(profile);
    }

    private async Task<UserProfile> GetRequiredProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _dbContext.UserProfiles
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile is not null)
        {
            return profile;
        }

        throw new AppException(StatusCodes.Status404NotFound, "profile_not_found", "Profile was not found.");
    }

    private static MyProfileResponse Map(UserProfile profile)
    {
        return new MyProfileResponse
        {
            UserId = profile.UserId,
            Username = profile.Username,
            DisplayName = profile.DisplayName,
            City = profile.City,
            Bio = profile.Bio,
            AvatarUrl = profile.AvatarUrl,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }

    private static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_username", "Username must be between 3 and 50 characters.");
        }

        var valid = username.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '.');
        if (!valid)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_username", "Username contains invalid characters.");
        }
    }

    private static void ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 100)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_display_name", "Display name is invalid.");
        }
    }

    private static void ValidateCity(string city)
    {
        if (city.Length > 100)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_city", "City is too long.");
        }
    }

    private static void ValidateBio(string bio)
    {
        if (bio.Length > 500)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_bio", "Bio is too long.");
        }
    }

    private static void ValidateAvatarUrl(string avatarUrl)
    {
        if (avatarUrl.Length > 500)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_avatar_url", "Avatar URL is too long.");
        }
    }
}