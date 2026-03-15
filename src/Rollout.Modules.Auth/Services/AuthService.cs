using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rollout.Modules.Auth.Data;
using Rollout.Modules.Auth.Dtos;
using Rollout.Modules.Auth.Entities;
using Rollout.Modules.Users.Data;
using Rollout.Modules.Users.Entities;
using Rollout.Shared.Auth;
using Rollout.Shared.Exceptions;

namespace Rollout.Modules.Auth.Services;

public sealed class AuthService
{
    private readonly AuthDbContext _dbContext;
    private readonly UsersDbContext _usersDbContext;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        AuthDbContext dbContext,
        UsersDbContext usersDbContext,
        PasswordHasher passwordHasher,
        JwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _usersDbContext = usersDbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<AuthTokensResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var password = request.Password?.Trim() ?? string.Empty;

        ValidateEmail(email);
        ValidatePassword(password);

        var exists = await _dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            throw new AppException(StatusCodes.Status409Conflict, "email_taken", "Email is already registered.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var passwordData = _passwordHasher.Hash(password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordData.Hash,
            PasswordSalt = passwordData.Salt,
            IsActive = true,
            CreatedAtUtc = now
        };

        var username = await GenerateDefaultUsernameAsync(user.Id, cancellationToken);

        var profile = new UserProfile
        {
            UserId = user.Id,
            Username = username,
            DisplayName = username,
            CreatedAtUtc = now
        };

        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshTokenValue),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_jwtOptions.RefreshTokenLifetimeDays)
        };

        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(refreshTokenEntity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            _usersDbContext.UserProfiles.Add(profile);
            await _usersDbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        return new AuthTokensResponse
        {
            AccessToken = accessToken.AccessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAtUtc = accessToken.ExpiresAtUtc
        };
    }

    public async Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var password = request.Password?.Trim() ?? string.Empty;

        ValidateEmail(email);
        ValidatePassword(password);

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new AppException(StatusCodes.Status401Unauthorized, "invalid_credentials", "Invalid credentials.");
        }

        var passwordValid = _passwordHasher.Verify(password, user.PasswordSalt, user.PasswordHash);
        if (!passwordValid)
        {
            throw new AppException(StatusCodes.Status401Unauthorized, "invalid_credentials", "Invalid credentials.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshTokenValue),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_jwtOptions.RefreshTokenLifetimeDays)
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        return new AuthTokensResponse
        {
            AccessToken = accessToken.AccessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAtUtc = accessToken.ExpiresAtUtc
        };
    }

    public async Task<AuthTokensResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var refreshTokenValue = request.RefreshToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "refresh_token_required", "Refresh token is required.");
        }

        var refreshTokenHash = HashToken(refreshTokenValue);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var existingToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

        if (existingToken is null ||
            existingToken.RevokedAtUtc is not null ||
            existingToken.ExpiresAtUtc <= now ||
            !existingToken.User.IsActive)
        {
            throw new AppException(StatusCodes.Status401Unauthorized, "invalid_refresh_token", "Refresh token is invalid.");
        }

        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = HashToken(newRefreshTokenValue);

        existingToken.RevokedAtUtc = now;
        existingToken.ReplacedByTokenHash = newRefreshTokenHash;

        var replacementToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existingToken.UserId,
            TokenHash = newRefreshTokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_jwtOptions.RefreshTokenLifetimeDays)
        };

        _dbContext.RefreshTokens.Add(replacementToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(existingToken.User);

        return new AuthTokensResponse
        {
            AccessToken = accessToken.AccessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAtUtc = accessToken.ExpiresAtUtc
        };
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var refreshTokenValue = request.RefreshToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "refresh_token_required", "Refresh token is required.");
        }

        var tokenHash = HashToken(refreshTokenValue);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var token = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (token is null)
        {
            return;
        }

        if (token.RevokedAtUtc is null)
        {
            token.RevokedAtUtc = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MeResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken);

        if (user is null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "user_not_found", "User was not found.");
        }

        return new MeResponse
        {
            UserId = user.Id,
            Email = user.Email
        };
    }

    private async Task<string> GenerateDefaultUsernameAsync(Guid userId, CancellationToken cancellationToken)
    {
        var baseValue = $"user{userId:N}"[..12];

        var exists = await _usersDbContext.UserProfiles
            .AnyAsync(x => x.Username == baseValue, cancellationToken);

        if (!exists)
        {
            return baseValue;
        }

        for (var i = 1; i <= 1000; i++)
        {
            var candidate = $"{baseValue}{i}";
            var taken = await _usersDbContext.UserProfiles
                .AnyAsync(x => x.Username == candidate, cancellationToken);

            if (!taken)
            {
                return candidate;
            }
        }

        throw new AppException(StatusCodes.Status500InternalServerError, "username_generation_failed", "Failed to generate username.");
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 || !email.Contains('@'))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_email", "Email is invalid.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 128)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "invalid_password", "Password must be between 8 and 128 characters.");
        }
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}