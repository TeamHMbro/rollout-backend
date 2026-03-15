using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rollout.Modules.Auth.Data;
using Rollout.Modules.Auth.Endpoints;
using Rollout.Modules.Auth.Services;
using Rollout.Shared.Auth;

namespace Rollout.Modules.Auth;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Auth"))
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.JwtKey) &&
                !string.IsNullOrWhiteSpace(options.JwtIssuer) &&
                !string.IsNullOrWhiteSpace(options.JwtAudience) &&
                options.AccessTokenLifetimeMinutes > 0 &&
                options.RefreshTokenLifetimeDays > 0,
                "Auth configuration is invalid.")
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString("AuthDb")
                               ?? throw new InvalidOperationException("ConnectionStrings:AuthDb is missing.");

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "auth")));

        services.AddScoped<PasswordHasher>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<AuthService>();

        return services;
    }

    public static IEndpointRouteBuilder MapAuthModule(this IEndpointRouteBuilder builder)
    {
        builder.MapAuthEndpoints();
        return builder;
    }
}