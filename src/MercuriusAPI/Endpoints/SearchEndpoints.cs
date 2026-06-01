using Asp.Versioning;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Extensions;
using Mercurius.LAN.API.Services.SearchServices;

namespace Mercurius.LAN.API.Endpoints;

public static class SearchEndpoints
{
    public static RouteGroupBuilder MapSearchEndpoints(this WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("v{version:apiVersion}/lan/search")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Search");

        group.MapGet("/", async (string? query, string? cursor, int? pageSize, ISearchService searchService, CancellationToken cancellationToken) =>
        {
            var normalizedQuery = (query ?? string.Empty).Trim();
            if (normalizedQuery.Length > SearchRequestLimits.MaximumQueryLength)
                throw new ValidationException($"Query cannot exceed {SearchRequestLimits.MaximumQueryLength} characters.");

            if (pageSize is <= 0)
                throw new ValidationException("pageSize must be greater than 0.");

            var boundedPageSize = Math.Min(pageSize ?? SearchRequestLimits.DefaultPageSize, SearchRequestLimits.MaximumPageSize);

            return await searchService.SearchAsync(query, cursor, boundedPageSize, cancellationToken);
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.AnonymousSearch);

        return group;
    }
}
