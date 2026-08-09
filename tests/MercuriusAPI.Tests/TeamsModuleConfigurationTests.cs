using System.Reflection;
using Mercurius.LAN.API.Data;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams;
using Mercurius.Modules.Teams.Contracts;
using Mercurius.Modules.Teams.DTOs;
using Mercurius.Modules.Teams.Infrastructure;
using Mercurius.Modules.Teams.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Realtime;

namespace Mercurius.LAN.API.Tests;

public class TeamsModuleConfigurationTests
{
    [Fact]
    public void AddTeamsModule_RegistersExpectedLifetimesAndDecoratedServices()
    {
        var services = CreateServiceCollection();

        AssertLifetime<ITeamsDbContext>(services, ServiceLifetime.Scoped);
        AssertLifetime<ITeamsModule>(services, ServiceLifetime.Transient);
        AssertLifetime<ITeamService>(services, ServiceLifetime.Transient);
        AssertLifetime<ITeamEndpointService>(services, ServiceLifetime.Transient);
        AssertLifetime<ITeamEventPublisher>(services, ServiceLifetime.Transient);
        AssertLifetime<Mercurius.Modules.Teams.Contracts.ITeamRealtimeAuthorizer>(services, ServiceLifetime.Transient);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var endpointService = scope.ServiceProvider.GetRequiredService<ITeamEndpointService>();
        var teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();
        var teamsModule = scope.ServiceProvider.GetRequiredService<ITeamsModule>();

        Assert.IsType<TeamEndpointService>(endpointService);
        Assert.IsType<TeamEventPublishingDecorator>(teamService);
        Assert.IsType<TeamsModuleFacade>(teamsModule);

        var innerService = typeof(TeamEventPublishingDecorator)
            .GetField("_inner", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(teamService);

        Assert.IsType<TeamService>(innerService);
    }

    [Fact]
    public void AddTeamsModule_UsesScopedAdapterAndKeepsHostDbContextOutsideTeamsContract()
    {
        var services = CreateServiceCollection();
        using var provider = services.BuildServiceProvider();

        using var firstScope = provider.CreateScope();
        var firstAdapter = firstScope.ServiceProvider.GetRequiredService<ITeamsDbContext>();
        var firstAdapterAgain = firstScope.ServiceProvider.GetRequiredService<ITeamsDbContext>();

        using var secondScope = provider.CreateScope();
        var secondAdapter = secondScope.ServiceProvider.GetRequiredService<ITeamsDbContext>();

        Assert.IsType<TeamsDbContextAdapter<MercuriusDBContext>>(firstAdapter);
        Assert.Same(firstAdapter, firstAdapterAgain);
        Assert.NotSame(firstAdapter, secondAdapter);
        Assert.DoesNotContain(typeof(ITeamsDbContext), typeof(MercuriusDBContext).GetInterfaces());
    }

    [Fact]
    public void AddTeamsModule_AllowsHostCompetitionReadOverrideAndSafeRepeatedTryAddRegistrations()
    {
        var services = CreateServiceCollection();
        services.AddTeamsModule<MercuriusDBContext>(CreateConfiguration());

        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(ITeamsDbContext)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(ITeamCompetitionReadService)));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var competitionReadService = scope.ServiceProvider.GetRequiredService<ITeamCompetitionReadService>();

        Assert.IsType<NullTeamCompetitionReadService>(competitionReadService);
    }

    [Fact]
    public void TeamsAsyncInterfaces_RequireTrailingCancellationToken()
    {
        var interfaces = new[]
        {
            typeof(ITeamsModule),
            typeof(ITeamCompetitionReadService),
            typeof(ITeamEventPublisher),
            typeof(Mercurius.Modules.Teams.Contracts.ITeamRealtimeAuthorizer),
            typeof(ITeamService),
            typeof(ITeamEndpointService)
        };

        foreach (var interfaceType in interfaces)
        {
            var asyncMethods = interfaceType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType));

            foreach (var method in asyncMethods)
            {
                var parameters = method.GetParameters();
                Assert.Equal(typeof(CancellationToken), parameters.Last().ParameterType);
                Assert.True(parameters.Last().HasDefaultValue, $"{interfaceType.Name}.{method.Name} should default its cancellation token.");
            }
        }
    }

    [Fact]
    public async Task TeamEndpointService_ForwardsCancellationTokenToRepresentativeWrite()
    {
        var spy = new RecordingTeamService();
        var endpointService = new TeamEndpointService(spy);
        using var cancellationSource = new CancellationTokenSource();

        await endpointService.DeleteTeamAsync("auth0|captain", Guid.NewGuid(), cancellationSource.Token);

        Assert.Equal(cancellationSource.Token, spy.DeleteTeamCancellationToken);
    }

    [Fact]
    public async Task RealtimeTeamEventPublisher_ForwardsCancellationToken()
    {
        var realtimePublisher = new RecordingRealtimePublisher();
        var publisher = new RealtimeTeamEventPublisher(realtimePublisher);
        using var cancellationSource = new CancellationTokenSource();

        await publisher.InviteChangedAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Pending", cancellationSource.Token);

        Assert.Equal(cancellationSource.Token, realtimePublisher.LastCancellationToken);
    }

    [Fact]
    public async Task TeamsModuleFacade_PreCancelledRead_ThrowsOperationCanceledException()
    {
        var services = CreateServiceCollection();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var teamsModule = scope.ServiceProvider.GetRequiredService<ITeamsModule>();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            teamsModule.GetTeamSummaryAsync(new TeamId(Guid.NewGuid()), cancellationSource.Token));
    }

    private static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<MercuriusDBContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IIdentityModule, StubIdentityModule>();
        services.AddSingleton<IRealtimePublisher, RecordingRealtimePublisher>();
        services.AddTeamsModule<MercuriusDBContext>(configuration);

        return services;
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TeamInvite:ResendCooldownDays"] = "7",
                ["TeamInvite:ExpirationDays"] = "14",
                ["TeamInvite:RetentionDays"] = "90",
                ["TeamInvite:DeclinedResendLimit"] = "3",
                ["FileStorage:MaxFileSizeInMB"] = "2",
                ["FileStorage:Location"] = Path.Combine(Path.GetTempPath(), "teams-module-tests")
            })
            .Build();
    }

    private static void AssertLifetime<TService>(IServiceCollection services, ServiceLifetime expectedLifetime)
    {
        var descriptor = services.Last(service => service.ServiceType == typeof(TService));
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }

    private sealed class RecordingRealtimePublisher : IRealtimePublisher
    {
        public CancellationToken LastCancellationToken { get; private set; }

        public Task PublishAsync<TPayload>(
            RealtimePublishRequest<TPayload> request,
            CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class StubIdentityModule : IIdentityModule
    {
        public Task<UserProfileSummary?> GetUserProfileAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserProfileSummary?>(new UserProfileSummary(userId, "captain", "captain", false, null, null, null));
        }

        public Task<UserProfileSummary?> GetUserProfileByAuth0IdAsync(string auth0UserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserProfileSummary?>(new UserProfileSummary(new UserId(Guid.NewGuid()), "captain", "captain", false, null, null, null));
        }

        public Task<PublicUserProfileSummary?> GetPublicProfileByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PublicUserProfileSummary?>(null);
        }

        public Task<IReadOnlyDictionary<UserId, UserProfileSummary>> GetUsersByIdsAsync(
            IReadOnlyCollection<UserId> userIds,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<UserId, UserProfileSummary> users = new Dictionary<UserId, UserProfileSummary>();
            return Task.FromResult(users);
        }

        public Task<IReadOnlyList<PublicUserSearchDocument>> GetPublicUserSearchDocumentsPageAsync(
            UserId? afterId,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicUserSearchDocument>>([]);
    }

    private sealed class RecordingTeamService : ITeamService
    {
        public CancellationToken DeleteTeamCancellationToken { get; private set; }

        public Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO, CancellationToken cancellationToken = default) => Task.FromResult(new GetTeamDTO());
        public Task<TeamManagementSummaryDTO> CreateCurrentUserTeamAsync(string auth0UserId, CreateTeamDTO teamDTO, CancellationToken cancellationToken = default) => Task.FromResult(new TeamManagementSummaryDTO());
        public Task DeleteTeamAsync(Guid teamId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default)
        {
            DeleteTeamCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<GetTeamDTO>> GetAllTeamsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<GetTeamDTO>>([]);
        public Task<CurrentUserTeamSummaryDTO> GetCurrentUserTeamSummaryAsync(string auth0UserId, CancellationToken cancellationToken = default) => Task.FromResult(new CurrentUserTeamSummaryDTO());
        public Task<PublicTeamProfileDTO> GetPublicTeamProfileAsync(string teamName, CancellationToken cancellationToken = default) => Task.FromResult(new PublicTeamProfileDTO());
        public Task<IEnumerable<TeamInviteDTO>> GetUserInvitesAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<TeamInviteDTO>>([]);
        public Task<IEnumerable<TeamInviteSummaryDTO>> GetCurrentUserInvitesAsync(string auth0UserId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<TeamInviteSummaryDTO>>([]);
        public Task<IEnumerable<TeamInviteSummaryDTO>> GetCurrentUserSentInvitesAsync(string auth0UserId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<TeamInviteSummaryDTO>>([]);
        public Task<GetTeamDTO> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult(new GetTeamDTO());
        public Task<GetTeamDTO> GetTeamByNameAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(new GetTeamDTO());
        public Task<TeamInviteDTO> InviteUserAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(new TeamInviteDTO());
        public Task<TeamInviteDTO> InviteUserAsync(string auth0UserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(new TeamInviteDTO());
        public Task<TeamInviteDTO> CancelInviteAsync(string auth0UserId, Guid teamId, Guid inviteId, CancellationToken cancellationToken = default) => Task.FromResult(new TeamInviteDTO());
        public Task<GetTeamDTO> RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(new GetTeamDTO());
        public Task<TeamManagementSummaryDTO> RemoveMemberAsync(string auth0UserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(new TeamManagementSummaryDTO());
        public Task<TeamManagementSummaryDTO> LeaveTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult(new TeamManagementSummaryDTO());
        public Task<TeamInviteDTO> RespondToInviteAsync(Guid teamId, Guid userId, bool accept, CancellationToken cancellationToken = default) => Task.FromResult(new TeamInviteDTO());
        public Task<TeamInviteDTO> RespondToInviteAsync(string auth0UserId, Guid inviteId, bool accept, CancellationToken cancellationToken = default) => Task.FromResult(new TeamInviteDTO());
        public Task<TeamManagementSummaryDTO> TransferCaptainAsync(string auth0UserId, Guid teamId, Guid newCaptainUserId, CancellationToken cancellationToken = default) => Task.FromResult(new TeamManagementSummaryDTO());
        public Task<TeamLogoResponseDTO> UploadTeamLogoAsync(string auth0UserId, Guid teamId, IFormFile logo, CancellationToken cancellationToken = default) => Task.FromResult(new TeamLogoResponseDTO(Guid.Empty, null));
        public Task<TeamLogoResponseDTO> RemoveTeamLogoAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult(new TeamLogoResponseDTO(Guid.Empty, null));
        public Task<IEnumerable<GetTeamDTO>> SearchTeamsByNameAsync(string query, int? limit = null, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<GetTeamDTO>>([]);
        public Task<GetTeamDTO> UpdateTeamAsync(Guid id, UpdateTeamDTO teamDTO, CancellationToken cancellationToken = default) => Task.FromResult(new GetTeamDTO());
    }
}
