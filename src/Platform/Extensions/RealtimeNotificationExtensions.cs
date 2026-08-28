using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Platform.Realtime;

namespace Platform.Extensions;

public static class RealtimeNotificationExtensions
{
    public static IServiceCollection AddRealtimeNotificationServices(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }

    public static IServiceCollection AddRealtimeNotificationServices<THub>(
        this IServiceCollection services,
        Action<HubOptions<THub>>? configureHub = null)
        where THub : Hub
    {
        var signalRBuilder = services.AddSignalR();
        if (configureHub is not null)
            signalRBuilder.AddHubOptions(configureHub);

        services.AddTransient<IRealtimePublisher, SignalRRealtimePublisher<THub>>();
        services.TryAddSingleton<IRealtimeConnectionManager, SignalRRealtimeConnectionManager<THub>>();
        return services;
    }
}
