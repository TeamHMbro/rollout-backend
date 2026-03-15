using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rollout.Modules.Auth.Data;
using Rollout.Modules.Events.Data;
using Rollout.Modules.Users.Data;

namespace Rollout.IntegrationTests.Infrastructure;

public sealed class RolloutApiFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _authDatabaseRoot = new();
    private readonly InMemoryDatabaseRoot _coreDatabaseRoot = new();

    private readonly IServiceProvider _authEfServiceProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    private readonly IServiceProvider _coreEfServiceProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuthDb"] = "Host=localhost;Database=ignored_auth_db;Username=ignored;Password=ignored",
                ["ConnectionStrings:CoreDb"] = "Host=localhost;Database=ignored_core_db;Username=ignored;Password=ignored"
            });
        });

        builder.ConfigureServices(services =>
        {
            ReplaceAuthDbContext(services);
            ReplaceUsersDbContext(services);
            ReplaceEventsDbContext(services);

            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();

            scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.EnsureCreated();
            scope.ServiceProvider.GetRequiredService<UsersDbContext>().Database.EnsureCreated();
            scope.ServiceProvider.GetRequiredService<EventsDbContext>().Database.EnsureCreated();
        });
    }

    private void ReplaceAuthDbContext(IServiceCollection services)
    {
        var authDbName = $"rollout-auth-tests-{Guid.NewGuid():N}";

        services.RemoveAll<AuthDbContext>();
        services.RemoveAll<DbContextOptions<AuthDbContext>>();
        services.RemoveAll(typeof(IDbContextOptionsConfiguration<AuthDbContext>));

        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseInMemoryDatabase(authDbName, _authDatabaseRoot)
                .UseInternalServiceProvider(_authEfServiceProvider)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        });
    }

    private void ReplaceUsersDbContext(IServiceCollection services)
    {
        var coreDbName = $"rollout-core-tests-{Guid.NewGuid():N}";

        services.RemoveAll<UsersDbContext>();
        services.RemoveAll<DbContextOptions<UsersDbContext>>();
        services.RemoveAll(typeof(IDbContextOptionsConfiguration<UsersDbContext>));

        services.AddDbContext<UsersDbContext>(options =>
        {
            options.UseInMemoryDatabase(coreDbName, _coreDatabaseRoot)
                .UseInternalServiceProvider(_coreEfServiceProvider)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        });
    }

    private void ReplaceEventsDbContext(IServiceCollection services)
    {
        var coreDbName = $"rollout-core-tests-{Guid.NewGuid():N}";

        services.RemoveAll<EventsDbContext>();
        services.RemoveAll<DbContextOptions<EventsDbContext>>();
        services.RemoveAll(typeof(IDbContextOptionsConfiguration<EventsDbContext>));

        services.AddDbContext<EventsDbContext>(options =>
        {
            options.UseInMemoryDatabase(coreDbName, _coreDatabaseRoot)
                .UseInternalServiceProvider(_coreEfServiceProvider)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        });
    }
}