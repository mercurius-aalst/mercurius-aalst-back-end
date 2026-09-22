using Mercurius.Modules.Tournament;
using Mercurius.Modules.Tournament.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.Api.Tests;

public sealed class LeaderboardEndpointRouteTests
{
    private const string Prefix = "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/leaderboard";

    [Fact]
    public void PublicLeaderboard_IsAnonymous()
    {
        var endpoint = GetEndpoint("GET", Prefix + "/");

        Assert.Contains(endpoint.Metadata, item => item is IAllowAnonymous);
    }

    [Theory]
    [InlineData("GET", Prefix + "/admin")]
    [InlineData("POST", Prefix + "/attempts")]
    [InlineData("PUT", Prefix + "/attempts/{attemptId:guid}")]
    [InlineData("DELETE", Prefix + "/attempts/{attemptId:guid}")]
    public void ManagementRoutes_RequireAdmin(string method, string pattern)
    {
        var endpoint = GetEndpoint(method, pattern);
        var authorization = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();

        Assert.DoesNotContain(endpoint.Metadata, item => item is IAllowAnonymous);
        Assert.Contains(authorization, item => item.Roles == "admin");
    }

    private static RouteEndpoint GetEndpoint(string method, string pattern)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddScoped<ITournamentQueries>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentManagementCommands>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentLifecycleCommands>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentRegistrationService>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<IMatchService>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ILeaderboardService>(_ => throw new NotSupportedException());
        var app = builder.Build();
        app.MapTournamentModule();

        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == pattern && endpoint.Metadata
                .OfType<IHttpMethodMetadata>().Any(item => item.HttpMethods.Contains(method)));
    }
}
