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

    public static IServiceCollection AddRealtimeNotificationServices<THub>(this IServiceCollection services)
        where THub : Hub
    {
        services.AddRealtimeNotificationServices();
        services.AddTransient<IRealtimePublisher, SignalRRealtimePublisher<THub>>();
        services.TryAddSingleton<IRealtimeConnectionManager, SignalRRealtimeConnectionManager<THub>>();
        return services;
    }
}
