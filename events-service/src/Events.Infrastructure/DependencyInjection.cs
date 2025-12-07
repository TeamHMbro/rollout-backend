using Events.Application;
using Events.Application.Abstractions;
using Events.Application.Services;
using Events.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Events.Infrastructure.Repositories;

namespace Events.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEventInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EventDb")
                              ?? throw new InvalidOperationException("Connection string 'EventDb' not found");

        services.AddDbContext<EventDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventMemberRepository, EventMemberRepository>();
        services.AddScoped<ILikedPostRepository, LikedPostRepository>();
        services.AddScoped<ISavedPostRepository, SavedPostRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}
