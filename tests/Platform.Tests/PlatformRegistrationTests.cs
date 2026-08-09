using Platform;
using Platform.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

namespace Platform.Tests;

public class PlatformRegistrationTests
{
    [Fact]
    public void AddAuth0JwtAuthentication_PreservesAuth0JwtValidationSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:Authority"] = "https://example.auth0.com/",
                ["Auth0:Audience"] = "https://api.example.com",
                ["Auth0:RoleClaimType"] = "https://example.com/roles"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAuth0JwtAuthentication(configuration.GetSection("Auth0"));

        using var provider = services.BuildServiceProvider();
        var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authentication.DefaultAuthenticateScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authentication.DefaultChallengeScheme);
        Assert.Equal("https://example.auth0.com", jwt.Authority);
        Assert.Equal("https://api.example.com", jwt.Audience);
        Assert.False(jwt.MapInboundClaims);
        Assert.True(jwt.TokenValidationParameters.ValidateIssuer);
        Assert.True(jwt.TokenValidationParameters.ValidateAudience);
        Assert.True(jwt.TokenValidationParameters.ValidateLifetime);
        Assert.True(jwt.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.True(jwt.TokenValidationParameters.RequireSignedTokens);
        Assert.Equal("sub", jwt.TokenValidationParameters.NameClaimType);
        Assert.Equal("https://example.com/roles", jwt.TokenValidationParameters.RoleClaimType);
        Assert.Contains(SecurityAlgorithms.RsaSha256, jwt.TokenValidationParameters.ValidAlgorithms);
    }

    [Fact]
    public void AddWildcardSubdomainCors_PreservesWildcardSubdomainPolicy()
    {
        const string policyName = "AllowApplication";
        var services = new ServiceCollection();
        services.AddWildcardSubdomainCors(policyName, "https://*.mercurius-aalst.be");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = options.GetPolicy(policyName);

        Assert.NotNull(policy);
        Assert.True(policy.IsOriginAllowed("https://app.mercurius-aalst.be"));
        Assert.Contains("*", policy.Headers);
        Assert.Contains("*", policy.Methods);
    }

    [Fact]
    public void AddHttpConventions_RegistersRouteConstraintAndEnumSerialization()
    {
        var services = new ServiceCollection();
        services.AddHttpConventions();

        using var provider = services.BuildServiceProvider();
        var routeOptions = provider.GetRequiredService<IOptions<RouteOptions>>().Value;
        var jsonOptions = provider.GetRequiredService<IOptions<JsonOptions>>().Value;

        Assert.Equal(typeof(NonGuidRouteConstraint), routeOptions.ConstraintMap["nonguid"]);
        Assert.Contains(
            jsonOptions.SerializerOptions.Converters,
            converter => converter is JsonStringEnumConverter);
    }
}
