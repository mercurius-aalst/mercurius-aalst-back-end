using System.Net;
using System.Security.Claims;
using Platform;
using Platform.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Platform.Tests;

public class RateLimitingConfigurationTests
{
    [Fact]
    public async Task AddFixedWindowRateLimiting_RegistersGlobalLimiter()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:GlobalPermitLimit"] = "1",
                ["RateLimiting:WindowSeconds"] = "60"
            })
            .Build();
        var services = new ServiceCollection();
        var rateLimitingSection = configuration.GetSection("RateLimiting");
        services.AddFixedWindowRateLimiting(new FixedWindowRateLimitingOptions
        {
            GlobalPermitLimit = rateLimitingSection.GetValue("GlobalPermitLimit", 120),
            PolicyPermitLimit = rateLimitingSection.GetValue("SearchPermitLimit", 30),
            Window = TimeSpan.FromSeconds(rateLimitingSection.GetValue("WindowSeconds", 60)),
            UnconditionalPolicyName = "anonymous-search",
            ConditionalPolicyName = "authenticated-search",
            ConditionalQueryParameterName = "query"
        });
        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        using var firstLease = await options.GlobalLimiter!.AcquireAsync(httpContext);
        using var secondLease = await options.GlobalLimiter.AcquireAsync(httpContext);

        Assert.Equal(StatusCodes.Status429TooManyRequests, options.RejectionStatusCode);
        Assert.True(firstLease.IsAcquired);
        Assert.False(secondLease.IsAcquired);
    }

    [Fact]
    public async Task AddFixedWindowRateLimiting_PartitionsAuthenticatedAndAnonymousCallersAsConfigured()
    {
        var services = new ServiceCollection();
        services.AddFixedWindowRateLimiting(new FixedWindowRateLimitingOptions
        {
            GlobalPermitLimit = 1,
            PolicyPermitLimit = 1,
            Window = TimeSpan.FromMinutes(1),
            UnconditionalPolicyName = "anonymous-search",
            ConditionalPolicyName = "authenticated-search",
            ConditionalQueryParameterName = "query"
        });
        await using var provider = services.BuildServiceProvider();
        var limiter = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value.GlobalLimiter!;
        var firstAuthenticatedContext = CreateHttpContext(IPAddress.Parse("127.0.0.1"), "user-one");
        var sameAuthenticatedContext = CreateHttpContext(IPAddress.Parse("127.0.0.2"), "user-one");
        var differentAuthenticatedContext = CreateHttpContext(IPAddress.Parse("127.0.0.1"), "user-two");
        var firstAnonymousContext = CreateHttpContext(IPAddress.Parse("127.0.0.3"));
        var sameAnonymousContext = CreateHttpContext(IPAddress.Parse("127.0.0.3"));
        var differentAnonymousContext = CreateHttpContext(IPAddress.Parse("127.0.0.4"));

        using var firstAuthenticatedLease = await limiter.AcquireAsync(firstAuthenticatedContext);
        using var sameAuthenticatedLease = await limiter.AcquireAsync(sameAuthenticatedContext);
        using var differentAuthenticatedLease = await limiter.AcquireAsync(differentAuthenticatedContext);
        using var firstAnonymousLease = await limiter.AcquireAsync(firstAnonymousContext);
        using var sameAnonymousLease = await limiter.AcquireAsync(sameAnonymousContext);
        using var differentAnonymousLease = await limiter.AcquireAsync(differentAnonymousContext);

        Assert.True(firstAuthenticatedLease.IsAcquired);
        Assert.False(sameAuthenticatedLease.IsAcquired);
        Assert.True(differentAuthenticatedLease.IsAcquired);
        Assert.True(firstAnonymousLease.IsAcquired);
        Assert.False(sameAnonymousLease.IsAcquired);
        Assert.True(differentAnonymousLease.IsAcquired);
    }

    private static DefaultHttpContext CreateHttpContext(IPAddress remoteIpAddress, string? subject = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteIpAddress;
        if (subject is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)]));

        return context;
    }
}
