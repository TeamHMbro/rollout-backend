namespace Auth.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string? email, string? phone);
}
