using System.Net;
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
}
