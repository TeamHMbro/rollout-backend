namespace Rollout.Modules.Users.Dtos;

public sealed class UpdateMyProfileRequest
{
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? City { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}