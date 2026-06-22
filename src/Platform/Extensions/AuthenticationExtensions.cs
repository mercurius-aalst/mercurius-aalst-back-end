using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Platform.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuth0JwtAuthentication(
        this IServiceCollection services,
        IConfigurationSection auth0Settings)
    {
        var auth0Authority = auth0Settings["Authority"]?.TrimEnd('/');

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = auth0Authority;
            options.Audience = auth0Settings["Audience"];
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                NameClaimType = "sub",
                RoleClaimType = auth0Settings["RoleClaimType"],
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
            };
        });
        services.AddAuthorization();

        return services;
    }
}
