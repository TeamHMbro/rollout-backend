using Chat.Application.Abstractions;
using Chat.Application.Services;
using Chat.Infrastructure.Events;
using Chat.Infrastructure.Realtime;
using Chat.Infrastructure.Repositories;
using Chat.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddChatInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ChatDb") ?? throw new InvalidOperationException("ConnectionStrings:ChatDb is not configured");
        var eventsBaseUrl = configuration["EventsService:BaseUrl"] ?? throw new InvalidOperationException("EventsService:BaseUrl is not configured");

        services.AddDbContext<ChatDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IEventMessageRepository, EventMessageRepository>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IChatService, ChatService>();
        services.AddHttpClient<IEventAccessService, EventAccessService>(client =>
        {
            client.BaseAddress = new Uri(eventsBaseUrl);
        });
        services.AddScoped<IChatRealtimeNotifier, ChatRealtimeNotifier>();

        return services;
    }
}
