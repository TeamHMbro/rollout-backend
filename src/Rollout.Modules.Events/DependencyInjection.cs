using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rollout.Modules.Events.Data;
using Rollout.Modules.Events.Endpoints;
using Rollout.Modules.Events.Services;

namespace Rollout.Modules.Events;

public static class DependencyInjection
{
    public static IServiceCollection AddEventsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CoreDb")
                               ?? throw new InvalidOperationException("ConnectionStrings:CoreDb is missing.");

        services.AddDbContext<EventsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "events")));

        services.AddScoped<EventService>();

        return services;
    }

    public static IEndpointRouteBuilder MapEventsModule(this IEndpointRouteBuilder builder)
    {
        builder.MapEventEndpoints();
        return builder;
    }
}