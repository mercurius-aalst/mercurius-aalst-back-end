using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.Services.MatchServices;
using Microsoft.AspNetCore.Authorization;

namespace Mercurius.LAN.API.Endpoints;

public static class MatchEndpoints
{
    public static RouteGroupBuilder MapMatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("lan/matches")
            .WithTags("Matches")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        group.MapPut("/{id}", async (int id, UpdateMatchDTO updateMatchDTO, IMatchService matchService) =>
        {
            return await matchService.UpdateMatchAsync(id, updateMatchDTO);
        });

        group.MapGet("/{id}", async (int id, IMatchService matchService) =>
        {
            return new GetMatchDTO(await matchService.GetMatchByIdAsync(id));
        })
        .AllowAnonymous();

        return group;
    }
}
