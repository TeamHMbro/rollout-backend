namespace Auth.Domain.Users;

public class AuthProvider
{
    public long Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Provider { get; private set; }
    public string ProviderId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AuthProvider() { }

    public AuthProvider(Guid userId, string provider, string providerId)
    {
        UserId = userId;
        Provider = provider;
        ProviderId = providerId;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
