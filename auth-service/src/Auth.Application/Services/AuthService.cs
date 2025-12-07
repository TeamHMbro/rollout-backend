using Auth.Application.Abstractions;
using Auth.Application.AuthContracts;
using Auth.Domain;
using Auth.Domain.Tokens;
using Auth.Domain.Users;

namespace Auth.Application.Services;

public interface IAuthService
{
    Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct);
    Task<MeResponse?> GetMeAsync(Guid userId, CancellationToken ct);
}

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTimeProvider _clock;
    private readonly AuthOptions _options;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt,
        IDateTimeProvider clock,
        AuthOptions options)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _clock = clock;
        _options = options;
    }

    public async Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
            throw new DomainException("EmailOrPhoneRequired");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (await _users.EmailExistsAsync(request.Email, ct))
                throw new DomainException("EmailAlreadyExists");
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            if (await _users.PhoneExistsAsync(request.Phone, ct))
                throw new DomainException("PhoneAlreadyExists");
        }

        var id = Guid.NewGuid();
        var hash = _passwordHasher.Hash(request.Password);
        var user = new AuthUser(id, request.Email, request.Phone, hash);

        await _users.AddAsync(user, ct);

        var accessToken = _jwt.GenerateAccessToken(user.Id, user.Email, user.Phone);
        var refreshToken = await IssueRefreshTokenAsync(user.Id, ct);

        return new TokenResponse(accessToken, refreshToken);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        AuthUser? user = null;

        if (!string.IsNullOrWhiteSpace(request.Email))
            user = await _users.GetByEmailAsync(request.Email, ct);
        else if (!string.IsNullOrWhiteSpace(request.Phone))
            user = await _users.GetByPhoneAsync(request.Phone, ct);

        if (user is null)
            throw new DomainException("InvalidCredentials");

        if (!user.IsActive())
            throw new DomainException("UserBlocked");

        if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
            throw new DomainException("InvalidCredentials");

        var accessToken = _jwt.GenerateAccessToken(user.Id, user.Email, user.Phone);
        var refreshToken = await IssueRefreshTokenAsync(user.Id, ct);

        return new TokenResponse(accessToken, refreshToken);
    }

    public async Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var existing = await _refreshTokens.GetAsync(request.RefreshToken, ct);
        if (existing is null || !existing.IsActive())
            throw new DomainException("InvalidRefreshToken");

        existing.Revoke();

        var accessToken = _jwt.GenerateAccessToken(existing.UserId, null, null);
        var newRefreshToken = await IssueRefreshTokenAsync(existing.UserId, ct);

        await _refreshTokens.SaveChangesAsync(ct);

        return new TokenResponse(accessToken, newRefreshToken);
    }

    public async Task<MeResponse?> GetMeAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return null;

        return new MeResponse(user.Id, user.Email, user.Phone);
    }

    private async Task<string> IssueRefreshTokenAsync(Guid userId, CancellationToken ct)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var expires = _clock.UtcNow.AddDays(_options.RefreshTokenLifetimeDays);
        var entity = new RefreshToken(userId, token, expires);
        await _refreshTokens.AddAsync(entity, ct);
        await _refreshTokens.SaveChangesAsync(ct);
        return token;
    }
}
