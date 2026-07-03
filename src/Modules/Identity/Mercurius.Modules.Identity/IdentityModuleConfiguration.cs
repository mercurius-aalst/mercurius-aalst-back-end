using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mercurius.Modules.Identity.Endpoints;
using Mercurius.Modules.Identity.Options;
using Mercurius.Modules.Identity.Services;
using Mercurius.Modules.Identity.Services.Auth0;

namespace Mercurius.Modules.Identity;

public static class IdentityModuleConfiguration
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<Auth0ManagementOptions>(configuration.GetSection(Auth0ManagementOptions.SectionName));
        services.AddHttpClient<IAuth0ManagementService, Auth0ManagementService>();
        services.AddTransient<UserService>();
        services.AddTransient<IUserService>(serviceProvider =>
            new UserValidationService(serviceProvider.GetRequiredService<UserService>()));

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapUserEndpoints();
        return endpoints;
    }
}
