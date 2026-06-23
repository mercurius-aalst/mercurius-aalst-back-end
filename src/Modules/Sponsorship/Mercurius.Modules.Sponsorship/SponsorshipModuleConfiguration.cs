using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.Modules.Sponsorship;

public static class SponsorshipModuleConfiguration
{
    public static IServiceCollection AddSponsorshipModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapSponsorshipModule(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
