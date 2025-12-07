namespace Auth.Domain.Tokens;

public class RefreshToken
{
    public long Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private RefreshToken() { }

    public RefreshToken(Guid userId, string token, DateTimeOffset expiresAt)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActive() => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;

    public void Revoke()
    {
        RevokedAt = DateTimeOffset.UtcNow;
    }
}
