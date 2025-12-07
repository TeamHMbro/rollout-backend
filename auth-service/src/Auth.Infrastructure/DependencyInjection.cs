using Auth.Application;
using Auth.Application.Abstractions;
using Auth.Application.Services;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AuthDb")
                              ?? throw new InvalidOperationException("Connection string 'AuthDb' not found");

        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        var authOptions = configuration.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();
        services.AddSingleton(authOptions);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
