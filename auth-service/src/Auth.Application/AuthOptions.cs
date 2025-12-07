namespace Auth.Application;

public sealed class AuthOptions
{
    public string JwtIssuer { get; init; } = string.Empty;
    public string JwtAudience { get; init; } = string.Empty;
    public string JwtKey { get; init; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
    public int RefreshTokenLifetimeDays { get; init; } = 30;
}
