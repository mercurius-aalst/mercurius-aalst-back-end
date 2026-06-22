namespace Mercurius.Platform.Realtime;

public static class MercuriusRealtimeExtensions
{
    public static IServiceCollection AddMercuriusRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }
}
