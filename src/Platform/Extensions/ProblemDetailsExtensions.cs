using Microsoft.AspNetCore.Diagnostics;

namespace Platform.Extensions;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddApiProblemDetails<TExceptionHandler>(this IServiceCollection services)
        where TExceptionHandler : class, IExceptionHandler
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<TExceptionHandler>();

        return services;
    }

    public static IApplicationBuilder UseApiExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler();
    }
}
