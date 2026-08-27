using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using Mercurius.Modules.Tournament;
using Mercurius.Modules.Tournament.Application.DTOs.Registrations;
using Mercurius.Modules.Tournament.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercurius.Api.Tests;

public class TournamentRegistrationEndpointRouteTests
{
    private const string RosterEligibilityRoute = "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/teams/{teamId:guid}/roster/eligibility";
    private const string RosterSubmissionRoute = "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/teams/{teamId:guid}/roster";

    [Theory]
    [InlineData("GET", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/me")]
    [InlineData("GET", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/individual/eligibility")]
    [InlineData("GET", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/teams/{teamId:guid}/eligibility")]
    [InlineData("POST", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/teams/{teamId:guid}/roster/eligibility")]
    [InlineData("PUT", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/individual/me")]
    [InlineData("PUT", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/teams/{teamId:guid}/roster")]
    [InlineData("PATCH", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/roster-members/{rosterMemberId:guid}")]
    public void CurrentUserRegistrationRoutes_RequireAuthorization(string method, string routePattern)
    {
        var endpoint = GetRegistrationRouteEndpoint(method, routePattern);

        Assert.DoesNotContain(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
        Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData);
    }

    [Theory]
    [InlineData("GET", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/admin/")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/admin/users/{userId:guid}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/admin/teams/{teamId:guid}")]
    public void AdminRegistrationRoutes_RequireAdminAuthorization(string method, string routePattern)
    {
        var endpoint = GetRegistrationRouteEndpoint(method, routePattern);
        var authorizeMetadata = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();

        Assert.DoesNotContain(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
        Assert.Contains(authorizeMetadata, metadata => metadata.Roles == "admin");
    }

    [Theory]
    [InlineData("POST", "v{version:apiVersion}/lan/tournaments/{id}/users")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/tournaments/{id}/users/{userId}")]
    [InlineData("POST", "v{version:apiVersion}/lan/tournaments/{id}/teams")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/tournaments/{id}/teams/{teamId}")]
    public void LegacyTournamentParticipantMutationRoutes_AreRemoved(string method, string routePattern)
    {
        var endpoints = GetTournamentRouteEndpoints(method);

        Assert.DoesNotContain(endpoints, endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    [Theory]
    [InlineData("GET", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/eligibility/individual")]
    [InlineData("GET", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/eligibility/teams/{teamId:guid}")]
    [InlineData("POST", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/eligibility/teams/{teamId:guid}/roster")]
    [InlineData("POST", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/individual")]
    [InlineData("POST", "v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/registrations/roster-confirmations/{rosterMemberId:guid}/confirm")]
    public void ReplacedRegistrationActionRoutes_AreRemoved(string method, string routePattern)
    {
        var endpoints = GetRegistrationRouteEndpoints(method);

        Assert.DoesNotContain(endpoints, endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    [Fact]
    public async Task RosterEndpoints_RejectInvalidUserIdsBeforeServiceInvocation()
    {
        var service = new RecordingTournamentRegistrationService();
        await using var app = CreateRegistrationApp(service);
        await app.StartAsync();
        using var client = CreateClient(app);
        var duplicateId = Guid.NewGuid();
        IReadOnlyList<Guid>?[] invalidUserIds =
        [
            null,
            [Guid.Empty],
            [duplicateId, duplicateId],
            Enumerable.Range(0, 51).Select(_ => Guid.NewGuid()).ToArray()
        ];

        foreach (var (method, route) in new[] { ("POST", RosterEligibilityRoute), ("PUT", RosterSubmissionRoute) })
        {
            foreach (var userIds in invalidUserIds)
            {
                var response = await InvokeRosterEndpointAsync(client, method, route, userIds);

                Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
                Assert.Contains("\"userIds\"", response.Body, StringComparison.Ordinal);
            }
        }

        Assert.Equal(0, service.RosterEligibilityCallCount);
        Assert.Equal(0, service.RosterSubmissionCallCount);
    }

    [Fact]
    public async Task RosterEndpoints_ForwardValidUserIdsAndRouteTeam()
    {
        var service = new RecordingTournamentRegistrationService();
        await using var app = CreateRegistrationApp(service);
        await app.StartAsync();
        using var client = CreateClient(app);
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var eligibilityResponse = await InvokeRosterEndpointAsync(client, "POST", RosterEligibilityRoute, userIds);
        var submissionResponse = await InvokeRosterEndpointAsync(client, "PUT", RosterSubmissionRoute, userIds);

        Assert.Equal(StatusCodes.Status200OK, eligibilityResponse.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, submissionResponse.StatusCode);
        Assert.Equal(1, service.RosterEligibilityCallCount);
        Assert.Equal(1, service.RosterSubmissionCallCount);
        Assert.Equal(userIds, service.LastUserIds);
        Assert.Equal(submissionResponse.TeamId, service.LastSubmission?.TeamId);
    }

    [Fact]
    public async Task AdminRegistrationList_ValidatesAndNormalizesPagingBeforeServiceInvocation()
    {
        var service = new RecordingTournamentRegistrationService();
        await using var app = CreateRegistrationApp(service);
        await app.StartAsync();
        using var client = CreateClient(app);
        var tournamentId = Guid.NewGuid();
        var path = $"v1/lan/tournaments/{tournamentId}/registrations/admin";

        using var invalidResponse = await client.GetAsync($"{path}?page=0");
        Assert.Equal(StatusCodes.Status400BadRequest, (int)invalidResponse.StatusCode);
        Assert.Equal(0, service.AdminRegistrationCallCount);

        using var defaultResponse = await client.GetAsync(path);
        using var cappedResponse = await client.GetAsync($"{path}?page=2&pageSize=51");
        Assert.Equal(StatusCodes.Status200OK, (int)defaultResponse.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, (int)cappedResponse.StatusCode);
        Assert.Equal((tournamentId, 2, 50), service.LastAdminRegistrationRequest);
    }

    private static RouteEndpoint GetRegistrationRouteEndpoint(string method, string routePattern)
    {
        return GetRegistrationRouteEndpoints(method)
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    private static WebApplication CreateRegistrationApp(ITournamentRegistrationService registrationService)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddScoped<ITournamentQueries>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentManagementCommands>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentLifecycleCommands>(_ => throw new NotSupportedException());
        builder.Services.AddSingleton(registrationService);
        builder.Services.AddScoped<IMatchService>(_ => throw new NotSupportedException());

        var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "auth0|roster-test"),
                new Claim(ClaimTypes.Role, "admin")
            ], "Test"));
            await next(context);
        });
        app.MapTournamentModule();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!;
        return new HttpClient { BaseAddress = new Uri(Assert.Single(addresses.Addresses)) };
    }

    private static async Task<(int StatusCode, string Body, Guid TeamId)> InvokeRosterEndpointAsync(
        HttpClient client,
        string method,
        string routePattern,
        IReadOnlyList<Guid>? userIds)
    {
        var tournamentId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var path = routePattern
            .Replace("v{version:apiVersion}", "v1", StringComparison.Ordinal)
            .Replace("{tournamentId:guid}", tournamentId.ToString(), StringComparison.Ordinal)
            .Replace("{teamId:guid}", teamId.ToString(), StringComparison.Ordinal);
        var body = JsonSerializer.Serialize(new { teamId = Guid.NewGuid(), userIds });
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        using var response = await client.SendAsync(request);
        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync(), teamId);
    }

    private static IEnumerable<RouteEndpoint> GetRegistrationRouteEndpoints(string method)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddScoped<ITournamentQueries>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentManagementCommands>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentLifecycleCommands>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentRegistrationService>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<IMatchService>(_ => throw new NotSupportedException());

        var app = builder.Build();
        app.MapTournamentModule();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata
                .OfType<IHttpMethodMetadata>()
                .Any(metadata => metadata.HttpMethods.Contains(method)));
    }

    private static IEnumerable<RouteEndpoint> GetTournamentRouteEndpoints(string method)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddScoped<ITournamentQueries>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentManagementCommands>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentLifecycleCommands>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITournamentRegistrationService>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<IMatchService>(_ => throw new NotSupportedException());

        var app = builder.Build();
        app.MapTournamentModule();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata
                .OfType<IHttpMethodMetadata>()
                .Any(metadata => metadata.HttpMethods.Contains(method)));
    }

    private sealed class RecordingTournamentRegistrationService : ITournamentRegistrationService
    {
        public int RosterEligibilityCallCount { get; private set; }
        public int RosterSubmissionCallCount { get; private set; }
        public int AdminRegistrationCallCount { get; private set; }
        public IReadOnlyList<Guid>? LastUserIds { get; private set; }
        public SubmitTeamRosterDTO? LastSubmission { get; private set; }
        public (Guid TournamentId, int Page, int PageSize) LastAdminRegistrationRequest { get; private set; }

        public Task<RosterCandidateEligibilityResponseDTO> CheckRosterEligibilityAsync(
            string auth0UserId,
            Guid tournamentId,
            Guid teamId,
            IReadOnlyList<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            RosterEligibilityCallCount++;
            LastUserIds = userIds;
            return Task.FromResult(new RosterCandidateEligibilityResponseDTO(true, [], []));
        }

        public Task<TournamentRegistrationDTO> SubmitTeamRosterAsync(
            string auth0UserId,
            Guid tournamentId,
            SubmitTeamRosterDTO request,
            CancellationToken cancellationToken = default)
        {
            RosterSubmissionCallCount++;
            LastUserIds = request.UserIds;
            LastSubmission = request;
            return Task.FromResult(new TournamentRegistrationDTO());
        }

        public Task<EligibilityResponseDTO> CheckIndividualEligibilityAsync(string auth0UserId, Guid tournamentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EligibilityResponseDTO> CheckTeamEligibilityAsync(string auth0UserId, Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TournamentRegistrationDTO> RegisterIndividualAsync(string auth0UserId, Guid tournamentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UnregisterIndividualAsync(string auth0UserId, Guid tournamentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TournamentRegistrationDTO> ConfirmRosterAsync(string auth0UserId, Guid tournamentId, Guid rosterMemberId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UnregisterTeamAsync(string auth0UserId, Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CurrentUserTournamentRegistrationStateDTO> GetCurrentUserStateAsync(string auth0UserId, Guid tournamentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AdminTournamentRegistrationDTO>> GetAdminRegistrationsAsync(Guid tournamentId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            AdminRegistrationCallCount++;
            LastAdminRegistrationRequest = (tournamentId, page, pageSize);
            return Task.FromResult<IReadOnlyList<AdminTournamentRegistrationDTO>>([]);
        }
        public Task RemoveIndividualAsAdminAsync(Guid tournamentId, Guid userId, string? reason, string? adminAuth0UserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveTeamAsAdminAsync(Guid tournamentId, Guid teamId, string? reason, string? adminAuth0UserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "auth0|roster-test"),
                new Claim(ClaimTypes.Role, "admin")
            ], Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
