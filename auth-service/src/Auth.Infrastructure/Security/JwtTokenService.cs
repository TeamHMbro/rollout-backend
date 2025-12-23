using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auth.Application.Abstractions;
using Auth.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateAccessToken(Guid userId, string? email, string? phone)
    {
        var authSection = _config.GetSection("Auth");
        var issuer = authSection.GetValue<string>("JwtIssuer");
        var audience = authSection.GetValue<string>("JwtAudience");
        var jwtKey = authSection.GetValue<string>("JwtKey") ??
                     throw new InvalidOperationException("Auth:JwtKey missing");

        var accessMinutes = authSection.GetValue<int?>("AccessTokenExpirationMinutes") ?? 60;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(phone))
        {
            claims.Add(new Claim("phone", phone));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(accessMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}