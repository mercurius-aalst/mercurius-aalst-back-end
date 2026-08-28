using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mercurius.Modules.Media.Contracts;
using Mercurius.Modules.Media.Infrastructure;

namespace Mercurius.Modules.Media;

public static class MediaModuleConfiguration
{
    public static IServiceCollection AddMediaModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<IMediaModule, FileSystemMediaModule>();
        return services;
    }
}
