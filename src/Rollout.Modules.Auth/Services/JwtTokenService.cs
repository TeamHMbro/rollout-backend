using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rollout.Modules.Auth.Entities;
using Rollout.Shared.Auth;

namespace Rollout.Modules.Auth.Services;

public sealed class JwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider)
    {
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public (string AccessToken, DateTime ExpiresAtUtc) GenerateAccessToken(User user)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.JwtIssuer,
            audience: _jwtOptions.JwtAudience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return (accessToken, expiresAtUtc);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}