namespace Platform.Extensions;

public static class SecurityPipelineExtensions
{
    public static IApplicationBuilder UseTransportSecurity(this IApplicationBuilder app, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            app.UseHsts();

        app.UseHttpsRedirection();

        return app;
    }

    public static IApplicationBuilder UseSecurityPipeline(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        return app;
    }
}
