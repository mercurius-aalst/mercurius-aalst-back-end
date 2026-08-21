using System.Text;
using System.Text.Json;
using Mercurius.Modules.Competition;
using Mercurius.Modules.Competition.Application.DTOs.Games;
using Mercurius.Modules.Competition.Application.Services;
using Mercurius.Modules.Teams;
using Mercurius.Modules.Teams.DTOs;
using Mercurius.Modules.Teams.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.Api.Tests;

public class PublicCollectionPagingEndpointTests
{
    private const string GameCollectionRoute = "v{version:apiVersion}/lan/games/";
    private const string TeamCollectionRoute = "v{version:apiVersion}/lan/teams/";

    [Theory]
    [InlineData("", 1, 20)]
    [InlineData("?page=2&pageSize=7", 2, 7)]
    [InlineData("?page=3&pageSize=51", 3, 50)]
    public async Task CollectionEndpoints_NormalizePagingAndReturnRawArrays(
        string queryString,
        int expectedPage,
        int expectedPageSize)
    {
        var gameQueries = new RecordingGameQueries();
        var teamService = new RecordingTeamEndpointService();
        await using var app = CreateApp(gameQueries, teamService);

        var gameResponse = await InvokeGetAsync(app, GameCollectionRoute, queryString);
        var teamResponse = await InvokeGetAsync(app, TeamCollectionRoute, queryString);

        Assert.Equal(StatusCodes.Status200OK, gameResponse.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, teamResponse.StatusCode);
        Assert.Equal((expectedPage, expectedPageSize), gameQueries.LastPaging);
        Assert.Equal((expectedPage, expectedPageSize), teamService.LastPaging);
        Assert.Equal(JsonValueKind.Array, JsonDocument.Parse(gameResponse.Body).RootElement.ValueKind);
        Assert.Equal(JsonValueKind.Array, JsonDocument.Parse(teamResponse.Body).RootElement.ValueKind);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=-1")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=-1")]
    public async Task CollectionEndpoints_RejectNonPositivePagingBeforeServiceInvocation(string queryString)
    {
        var gameQueries = new RecordingGameQueries();
        var teamService = new RecordingTeamEndpointService();
        await using var app = CreateApp(gameQueries, teamService);

        var gameResponse = await InvokeGetAsync(app, GameCollectionRoute, queryString);
        var teamResponse = await InvokeGetAsync(app, TeamCollectionRoute, queryString);

        Assert.Equal(StatusCodes.Status400BadRequest, gameResponse.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, teamResponse.StatusCode);
        Assert.Equal(0, gameQueries.CallCount);
        Assert.Equal(0, teamService.CallCount);
    }

    private static WebApplication CreateApp(
        IGameQueries gameQueries,
        ITeamEndpointService teamEndpointService)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddSingleton(gameQueries);
        builder.Services.AddScoped<IGameManagementCommands>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<IGameLifecycleCommands>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentRegistrationService>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<IMatchService>(_ => throw new NotSupportedException());
        builder.Services.AddSingleton(teamEndpointService);

        var app = builder.Build();
        app.MapCompetitionModule();
        app.MapTeamsModule();
        return app;
    }

    private static async Task<(int StatusCode, string Body)> InvokeGetAsync(
        WebApplication app,
        string routePattern,
        string queryString)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint =>
                endpoint.RoutePattern.RawText == routePattern &&
                endpoint.Metadata.OfType<IHttpMethodMetadata>()
                    .Any(metadata => metadata.HttpMethods.Contains("GET")));
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString(queryString);
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return (context.Response.StatusCode, await reader.ReadToEndAsync());
    }

    private sealed class RecordingGameQueries : IGameQueries
    {
        public int CallCount { get; private set; }
        public (int Page, int PageSize) LastPaging { get; private set; }

        public Task<IReadOnlyList<GetGameDTO>> GetAllGamesAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastPaging = (page, pageSize);
            return Task.FromResult<IReadOnlyList<GetGameDTO>>([new GetGameDTO { Id = Guid.NewGuid(), Name = "Paged game" }]);
        }

        public Task<GetGameDTO> GetGameByIdAsync(Guid gameId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingTeamEndpointService : ITeamEndpointService
    {
        public int CallCount { get; private set; }
        public (int Page, int PageSize) LastPaging { get; private set; }

        public Task<IReadOnlyList<TeamResponseDTO>> GetAllTeamsAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastPaging = (page, pageSize);
            return Task.FromResult<IReadOnlyList<TeamResponseDTO>>([new TeamResponseDTO { Id = Guid.NewGuid(), Name = "Paged team" }]);
        }

        public Task<TeamResponseDTO> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TeamManagementSummaryResponseDTO> CreateCurrentUserTeamAsync(string auth0UserId, CreateTeamRequestDTO request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CurrentUserTeamSummaryResponseDTO> GetCurrentUserTeamSummaryAsync(string auth0UserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<TeamInviteSummaryResponseDTO>> GetCurrentUserInvitesAsync(string auth0UserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<TeamInviteSummaryResponseDTO>> GetCurrentUserSentInvitesAsync(string auth0UserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TeamManagementSummaryResponseDTO> LeaveTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TeamManagementSummaryResponseDTO> RemoveMemberAsync(string auth0UserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TeamInviteResponseDTO> InviteUserAsync(string auth0UserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TeamInviteResponseDTO> CancelInviteAsync(string auth0UserId, Guid teamId, Guid inviteId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TeamInviteResponseDTO> RespondToInviteAsync(string auth0UserId, Guid inviteId, bool accept, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TeamManagementSummaryResponseDTO> TransferCaptainAsync(string auth0UserId, Guid teamId, Guid newCaptainUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TeamLogoResponseDTO> UploadTeamLogoAsync(string auth0UserId, Guid teamId, IFormFile logo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TeamLogoResponseDTO> RemoveTeamLogoAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PublicTeamProfileResponseDTO> GetPublicTeamProfileAsync(string teamName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
