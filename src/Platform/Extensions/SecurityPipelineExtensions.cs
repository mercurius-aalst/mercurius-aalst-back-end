namespace Platform.Extensions;

public static class SecurityPipelineExtensions
{
    public static IApplicationBuilder UseSecurityPipeline(this IApplicationBuilder app)
    {
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        return app;
    }
}
