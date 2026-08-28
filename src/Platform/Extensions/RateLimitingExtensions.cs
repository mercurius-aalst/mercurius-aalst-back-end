using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Platform.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddFixedWindowRateLimiting(
        this IServiceCollection services,
        FixedWindowRateLimitingOptions settings)
    {
        var globalPermitLimit = Math.Max(1, settings.GlobalPermitLimit);
        var policyPermitLimit = Math.Max(1, settings.PolicyPermitLimit);
        var window = settings.Window > TimeSpan.Zero ? settings.Window : TimeSpan.FromSeconds(1);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                CreateFixedWindowPartition(httpContext, globalPermitLimit, window, settings.UserIdentifierClaimType));
            options.AddPolicy(settings.UnconditionalPolicyName, httpContext =>
                CreateFixedWindowPartition(httpContext, policyPermitLimit, window, settings.UserIdentifierClaimType));
            options.AddPolicy(settings.ConditionalPolicyName, httpContext =>
                httpContext.Request.Query.ContainsKey(settings.ConditionalQueryParameterName)
                    ? CreateFixedWindowPartition(httpContext, policyPermitLimit, window, settings.UserIdentifierClaimType)
                    : RateLimitPartition.GetNoLimiter(GetPartitionKey(httpContext, settings.UserIdentifierClaimType)));
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = settings.RejectionMessage },
                    cancellationToken);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext httpContext,
        int permitLimit,
        TimeSpan window,
        string userIdentifierClaimType)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            GetPartitionKey(httpContext, userIdentifierClaimType),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    private static string GetPartitionKey(HttpContext httpContext, string userIdentifierClaimType)
    {
        var subject = httpContext.User.FindFirst(userIdentifierClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(subject))
            return $"user:{subject}";

        return $"ip:{httpContext.Connection.RemoteIpAddress}";
    }
}
