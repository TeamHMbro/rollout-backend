using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rollout.Modules.Auth.Data;
using Rollout.Modules.Events.Data;
using Rollout.Modules.Users.Data;

namespace Rollout.Api.HealthChecks;

public sealed class DatabaseHealthCheck(
    AuthDbContext authDbContext,
    UsersDbContext usersDbContext,
    EventsDbContext eventsDbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var authAvailable = await CanConnectAsync(authDbContext, cancellationToken);
        var usersAvailable = await CanConnectAsync(usersDbContext, cancellationToken);
        var eventsAvailable = await CanConnectAsync(eventsDbContext, cancellationToken);

        if (authAvailable && usersAvailable && eventsAvailable)
        {
            return HealthCheckResult.Healthy();
        }

        return HealthCheckResult.Unhealthy(data: new Dictionary<string, object>
        {
            ["authDb"] = authAvailable,
            ["usersDb"] = usersAvailable,
            ["eventsDb"] = eventsAvailable
        });
    }

    private static async Task<bool> CanConnectAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return true;
        }

        return await dbContext.Database.CanConnectAsync(cancellationToken);
    }
}