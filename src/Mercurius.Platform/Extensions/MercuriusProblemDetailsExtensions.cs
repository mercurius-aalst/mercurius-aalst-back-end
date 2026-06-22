using Microsoft.AspNetCore.Diagnostics;

namespace Mercurius.Platform.Extensions;

public static class MercuriusProblemDetailsExtensions
{
    public static IServiceCollection AddMercuriusProblemDetails<TExceptionHandler>(this IServiceCollection services)
        where TExceptionHandler : class, IExceptionHandler
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<TExceptionHandler>();

        return services;
    }

    public static IApplicationBuilder UseMercuriusExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler();
    }
}
