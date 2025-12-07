namespace Auth.Domain.Users;

public class AuthUser
{
    public Guid Id { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string PasswordHash { get; private set; }
    public string Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AuthUser() { }

    public AuthUser(Guid id, string? email, string? phone, string passwordHash)
    {
        if (email is null && phone is null)
            throw new DomainException("EmailOrPhoneRequired");

        Id = id;
        Email = email;
        Phone = phone;
        PasswordHash = passwordHash;
        Status = "active";
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void SetPasswordHash(string hash)
    {
        PasswordHash = hash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Block()
    {
        Status = "blocked";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActive() => Status == "active";
}
