namespace Mercurius.Platform.Cors;

public static class MercuriusCorsExtensions
{
    public const string PolicyName = "AllowMercuriusAalst";

    public static IServiceCollection AddMercuriusCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.WithOrigins("https://*.mercurius-aalst.be")
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseMercuriusCors(this IApplicationBuilder app)
    {
        return app.UseCors(PolicyName);
    }
}
