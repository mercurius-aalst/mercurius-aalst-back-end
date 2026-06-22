namespace Mercurius.Platform.Extensions;

public static class MercuriusRealtimeExtensions
{
    public static IServiceCollection AddMercuriusRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }
}
