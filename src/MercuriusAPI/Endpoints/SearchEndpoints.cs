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
            SearchRequest.ValidateQueryLength(SearchRequest.NormalizeQuery(query));
            SearchRequest.ValidatePageSize(pageSize);

            var boundedPageSize = SearchRequest.BoundPageSize(pageSize);

            return await searchService.SearchAsync(query, cursor, boundedPageSize, cancellationToken);
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.AnonymousSearch);

        return group;
    }
}
