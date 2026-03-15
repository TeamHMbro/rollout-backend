using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rollout.Modules.Users.Data;
using Rollout.Modules.Users.Endpoints;
using Rollout.Modules.Users.Services;

namespace Rollout.Modules.Users;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CoreDb")
                               ?? throw new InvalidOperationException("ConnectionStrings:CoreDb is missing.");

        services.AddDbContext<UsersDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "users")));

        services.AddScoped<UserProfileService>();

        return services;
    }

    public static IEndpointRouteBuilder MapUsersModule(this IEndpointRouteBuilder builder)
    {
        builder.MapUserEndpoints();
        return builder;
    }
}