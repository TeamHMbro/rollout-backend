namespace Events.Domain.Users;

public class UserProfile
{
    public Guid Id { get; private set; }
    public string UserName { get; private set; }
    public string? Avatar { get; private set; }
    public string? City { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private UserProfile() { }

    public UserProfile(Guid id, string userName, string? city, DateTimeOffset now)
    {
        Id = id;
        UserName = userName;
        City = city;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Update(string userName, string? avatar, string? city, DateTimeOffset now)
    {
        UserName = userName;
        Avatar = avatar;
        City = city;
        UpdatedAt = now;
    }
}
