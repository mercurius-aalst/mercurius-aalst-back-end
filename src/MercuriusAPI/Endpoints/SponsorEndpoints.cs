using Mercurius.LAN.API.DTOs.SponsorDTOs;
using Mercurius.LAN.API.Services.SponsorServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercurius.LAN.API.Endpoints;

public static class SponsorEndpoints
{
    public static RouteGroupBuilder MapSponsorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("api/v{version:apiVersion}/lan/sponsors")
            .WithGroupName("v1")
            .WithTags("Sponsors")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        group.MapGet("/", (ISponsorService sponsorService) =>
        {
            return sponsorService.GetSponsors();
        })
        .AllowAnonymous();

        group.MapGet("/{id}", async (int id, ISponsorService sponsorService) =>
        {
            return await sponsorService.GetSponsorByIdAsync(id);
        })
        .AllowAnonymous();

        group.MapPost("/", async ([FromForm] CreateSponsorDTO sponsorDTO, ISponsorService sponsorService) =>
        {
            return await sponsorService.CreateSponsorAsync(sponsorDTO);
        });

        group.MapPatch("/{id}", async (int id, [FromForm] UpdateSponsorDTO value, ISponsorService sponsorService) =>
        {
            return await sponsorService.UpdateSponsorAsync(id, value);
        });

        group.MapDelete("/{id}", async (int id, ISponsorService sponsorService) =>
        {
            await sponsorService.DeleteSponsorAsync(id);
        });

        return group;
    }
}
