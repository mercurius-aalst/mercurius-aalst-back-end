namespace Platform.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddWildcardSubdomainCors(
        this IServiceCollection services,
        string policyName,
        string allowedOrigin)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                policy.WithOrigins(allowedOrigin)
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
