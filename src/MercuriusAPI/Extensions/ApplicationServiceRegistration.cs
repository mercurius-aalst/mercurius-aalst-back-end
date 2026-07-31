using Mercurius.LAN.API.Composition;
using Mercurius.LAN.API.Services.Files;
using Mercurius.LAN.API.Services.SearchServices;
using Mercurius.Modules.Media.Contracts;

namespace Mercurius.LAN.API.Extensions;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddTransient<ISearchService, SearchService>();

        services.AddTransient<IFileService, FileService>();
        services.Decorate<IFileService, FileValidationService>();
        services.AddTransient<IMediaModule, LegacyMediaModuleAdapter>();

        return services;
    }
}
