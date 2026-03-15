namespace Rollout.Api.Seeding;

public sealed class DevSeedOptions
{
    public bool Enabled { get; set; }
    public string DefaultPassword { get; set; } = "Password123!";
}