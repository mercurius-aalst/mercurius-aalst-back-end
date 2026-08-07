using Mercurius.LAN.API.Services.SearchServices;

namespace Mercurius.LAN.API.Extensions;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddTransient<ISearchService, SearchService>();

        return services;
    }
}
