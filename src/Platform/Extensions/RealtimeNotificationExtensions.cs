namespace Platform.Extensions;

public static class RealtimeNotificationExtensions
{
    public static IServiceCollection AddRealtimeNotificationServices(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }
}
