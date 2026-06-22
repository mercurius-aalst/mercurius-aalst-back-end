namespace Mercurius.Platform.Security;

public static class MercuriusSecurityPipelineExtensions
{
    public static IApplicationBuilder UseMercuriusSecurityPipeline(this IApplicationBuilder app)
    {
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        return app;
    }
}
