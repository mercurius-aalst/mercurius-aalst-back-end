namespace Platform.Extensions;

public static class RealtimeExtensions
{
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }
}
