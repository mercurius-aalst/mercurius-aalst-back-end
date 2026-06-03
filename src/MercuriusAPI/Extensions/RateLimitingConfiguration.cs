using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Mercurius.LAN.API.Extensions;

public static class RateLimitPolicies
{
    public const string AnonymousSearch = "anonymous-search";
}

public static class RateLimitingConfiguration
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitingSection = configuration.GetSection("RateLimiting");
        var globalPermitLimit = Math.Max(1, rateLimitingSection.GetValue("GlobalPermitLimit", 120));
        var searchPermitLimit = Math.Max(1, rateLimitingSection.GetValue("SearchPermitLimit", 30));
        var window = TimeSpan.FromSeconds(Math.Max(1, rateLimitingSection.GetValue("WindowSeconds", 60)));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                CreateFixedWindowPartition(httpContext, globalPermitLimit, window));
            options.AddPolicy(RateLimitPolicies.AnonymousSearch, httpContext =>
                CreateFixedWindowPartition(httpContext, searchPermitLimit, window));
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Too many requests. Please try again later." },
                    cancellationToken);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(HttpContext httpContext, int permitLimit, TimeSpan window)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            GetPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    private static string GetPartitionKey(HttpContext httpContext)
    {
        var subject = httpContext.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(subject))
            return $"user:{subject}";

        return $"ip:{httpContext.Connection.RemoteIpAddress}";
    }
}
