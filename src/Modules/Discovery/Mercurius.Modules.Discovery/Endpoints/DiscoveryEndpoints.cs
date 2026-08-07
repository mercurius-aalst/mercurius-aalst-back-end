using Asp.Versioning;
using Mercurius.Modules.Discovery.Contracts;
using Mercurius.Modules.Shared.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Mercurius.Modules.Discovery.Endpoints;

internal static class DiscoveryEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var searchGroup = endpoints.MapGroup("v{version:apiVersion}/lan/search")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Search");

        searchGroup.MapGet("/", async (
                string? query,
                string? cursor,
                int? pageSize,
                IDiscoveryModule discoveryModule,
                CancellationToken cancellationToken) =>
            {
                SearchRequest.ValidateQueryLength(SearchRequest.NormalizeQuery(query));
                SearchRequest.ValidatePageSize(pageSize);

                return await discoveryModule.SearchAsync(
                    new DiscoverySearchRequest(query, cursor, SearchRequest.BoundPageSize(pageSize)),
                    cancellationToken);
            })
            .AllowAnonymous()
            .RequireRateLimiting(SearchRateLimitPolicyNames.Anonymous);

        var rebuildJobs = endpoints.MapGroup("/internal/discovery/search-index-rebuild-jobs")
            .WithTags("Discovery")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        rebuildJobs.MapPost("/", async (
                IDiscoveryModule discoveryModule,
                CancellationToken cancellationToken) =>
            {
                var job = await discoveryModule.CreateSearchIndexRebuildJobAsync(cancellationToken);
                return Results.Accepted($"/internal/discovery/search-index-rebuild-jobs/{job.Id}", job);
            });

        rebuildJobs.MapGet("/{jobId:guid}", async (
                Guid jobId,
                IDiscoveryModule discoveryModule,
                CancellationToken cancellationToken) =>
            {
                var job = await discoveryModule.GetSearchIndexRebuildJobAsync(jobId, cancellationToken);
                return job is null ? Results.NotFound() : Results.Ok(job);
            });
    }
}
