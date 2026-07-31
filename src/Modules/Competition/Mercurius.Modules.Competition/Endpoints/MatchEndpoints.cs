using Asp.Versioning;
using Mercurius.Modules.Competition.Application.DTOs.Matches;
using Mercurius.Modules.Competition.Application.Services;
using Microsoft.AspNetCore.Authorization;

namespace Mercurius.Modules.Competition.Endpoints;

internal static class MatchEndpoints
{
    internal static RouteGroupBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var apiVersionSet = app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(1, 0))
        .ReportApiVersions()
        .Build();
        var group = app.MapGroup("v{version:apiVersion}/lan/matches")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Matches")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        group.MapPut("/{id}", async (Guid id, UpdateMatchDTO updateMatchDTO, IMatchService matchService, CancellationToken cancellationToken) =>
        {
            return await matchService.UpdateMatchAsync(id, updateMatchDTO, cancellationToken);
        });

        group.MapGet("/{id}", async (Guid id, IMatchService matchService, CancellationToken cancellationToken) =>
        {
            return await matchService.GetMatchByIdAsync(id, cancellationToken);
        })
        .AllowAnonymous();

        return group;
    }
}
