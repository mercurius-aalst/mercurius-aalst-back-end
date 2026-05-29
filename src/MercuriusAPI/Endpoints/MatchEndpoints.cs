using Asp.Versioning;
using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.Services.MatchServices;
using Microsoft.AspNetCore.Authorization;

namespace Mercurius.LAN.API.Endpoints;

public static class MatchEndpoints
{
    public static RouteGroupBuilder MapMatchEndpoints(this WebApplication app)
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

        group.MapPut("/{id}", async (Guid id, UpdateMatchDTO updateMatchDTO, IMatchService matchService) =>
        {
            return await matchService.UpdateMatchAsync(id, updateMatchDTO);
        });

        group.MapGet("/{id}", async (Guid id, IMatchService matchService) =>
        {
            return new GetMatchDTO(await matchService.GetMatchByIdAsync(id));
        })
        .AllowAnonymous();

        return group;
    }
}
