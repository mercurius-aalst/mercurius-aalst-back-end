using Mercurius.LAN.API.Composition;
using Mercurius.LAN.API.Services.Files;
using Mercurius.LAN.API.Services.SearchServices;
using Mercurius.LAN.API.Services.SponsorServices;
using Mercurius.Modules.Media.Contracts;
using Mercurius.Modules.Sponsorship.Contracts;

namespace Mercurius.LAN.API.Extensions;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddTransient<ISponsorService, SponsorService>();
        services.AddTransient<ISearchService, SearchService>();

        services.AddTransient<IFileService, FileService>();
        services.Decorate<IFileService, FileValidationService>();
        services.AddTransient<IMediaModule, LegacyMediaModuleAdapter>();
        services.AddTransient<ISponsorshipModule, LegacySponsorshipModuleAdapter>();

        return services;
    }
}
