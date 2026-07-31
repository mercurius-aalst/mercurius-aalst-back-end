using System.Text;
using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Models;
using Mercurius.Modules.Competition.Application.DTOs.Games;
using Mercurius.LAN.API.DTOs.SponsorDTOs;
using Mercurius.LAN.API.Services.Files;
using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Media.Contracts;
using Mercurius.Modules.Competition.Application.Services;
using SponsorContractContext = Mercurius.Modules.Sponsorship.Contracts.SponsorContext;
using SponsorContractTier = Mercurius.Modules.Sponsorship.Contracts.SponsorTier;
using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.LAN.API.Services.SponsorServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Tests;

public class SponsorFeatureTests
{
    [Fact]
    public async Task CreateSponsorAsync_PersistsTierAndDescription()
    {
        await using var dbContext = CreateDbContext();
        var sponsorService = new SponsorService(dbContext, new StubFileService());

        var sponsor = await sponsorService.CreateSponsorAsync(new CreateSponsorDTO
        {
            Name = "Mercurius Tech",
            SponsorTier = SponsorTier.Presenting,
            InfoUrl = "https://example.test/mercurius-tech",
            Description = "Primary event partner.",
            Logo = CreateFormFile()
        });

        Assert.Equal(SponsorTier.Presenting, sponsor.SponsorTier);
        Assert.Equal("Primary event partner.", sponsor.Description);

        var storedSponsor = await dbContext.Sponsors.SingleAsync();
        Assert.Equal(SponsorTier.Presenting, storedSponsor.SponsorTier);
        Assert.Equal("Primary event partner.", storedSponsor.Description);
    }

    [Fact]
    public async Task UpdateSponsorAsync_UpdatesDescriptionAndTier()
    {
        await using var dbContext = CreateDbContext();
        var sponsorService = new SponsorService(dbContext, new StubFileService());
        var sponsor = new Sponsor
        {
            Name = "Campus Fiber",
            SponsorTier = SponsorTier.Silver,
            InfoUrl = "https://example.test/campus-fiber",
            LogoUrl = "/images/campus-fiber.png"
        };
        dbContext.Sponsors.Add(sponsor);
        await dbContext.SaveChangesAsync();

        var updatedSponsor = await sponsorService.UpdateSponsorAsync(sponsor.Id, new UpdateSponsorDTO
        {
            Name = "Campus Fiber",
            SponsorTier = SponsorTier.Gold,
            InfoUrl = "https://example.test/campus-fiber",
            Description = "Network backbone partner."
        });

        Assert.Equal(SponsorTier.Gold, updatedSponsor.SponsorTier);
        Assert.Equal("Network backbone partner.", updatedSponsor.Description);
    }

    [Fact]
    public async Task ReplaceSponsorPlacementsAsync_ReplacesExistingPlacementAndReturnsSponsorData()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateGame();
        var presentingSponsor = CreateSponsor(1, "Mercurius Tech", SponsorTier.Presenting);
        dbContext.Set<Game>().Add(game);
        dbContext.Sponsors.Add(presentingSponsor);
        await dbContext.SaveChangesAsync();

        var sponsorPlacement = CreateSponsorPlacementSummary(game.Id, presentingSponsor, SponsorContractContext.CateringPartner, null, null, 99);
        var sponsorshipModule = new RecordingSponsorshipModule(
            [presentingSponsor.Id],
            sponsorPlacement);
        var identityModule = CompetitionTestSupport.CreateIdentityModule();
        var teamsModule = CompetitionTestSupport.CreateTeamsModule();
        var service = new GameService(
            new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext),
            new StubMatchModeratorFactory(),
            new StubMediaModule(),
            sponsorshipModule,
            new CompetitionDtoMapper(
                new RegistrationMappingContextBuilder(identityModule, teamsModule),
                sponsorshipModule),
            CompetitionTestSupport.CreateModuleEventPublisher());
        var updatedGame = await service.ReplaceSponsorPlacementsAsync(game.Id, new ReplaceGameSponsorsDTO
        {
            SponsorPlacements =
            [
                new GameSponsorPlacementInputDTO
                {
                    SponsorId = presentingSponsor.Id,
                    Context = SponsorContractContext.TournamentPartner,
                    Headline = "Presented by Mercurius Tech",
                    SupportLine = "Main stage and stream support",
                    DisplayOrder = 1
                }
            ]
        });

        Assert.NotNull(updatedGame.SponsorPlacement);
        Assert.Equal(SponsorContractContext.TournamentPartner, updatedGame.SponsorPlacement.Context);
        Assert.Equal("Mercurius Tech", updatedGame.SponsorPlacement.SponsorName);
        Assert.Equal(SponsorContractTier.Presenting, updatedGame.SponsorPlacement.SponsorTier);
        Assert.Equal("Presented by Mercurius Tech", updatedGame.SponsorPlacement.Headline);
        Assert.NotNull(sponsorshipModule.ReplacedPlacement);
        Assert.Equal(SponsorContractContext.TournamentPartner, sponsorshipModule.ReplacedPlacement!.Context);
    }

    [Fact]
    public async Task ReplaceSponsorPlacementsAsync_ThrowsWhenMoreThanOneSponsorIsProvided()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateGame();
        var presentingSponsor = CreateSponsor(1, "Mercurius Tech", SponsorTier.Presenting);
        var prizeSponsor = CreateSponsor(2, "Campus Fiber", SponsorTier.Gold);
        dbContext.Set<Game>().Add(game);
        dbContext.Sponsors.AddRange(presentingSponsor, prizeSponsor);
        await dbContext.SaveChangesAsync();

        var service = new GameService(
            new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext),
            new StubMatchModeratorFactory(),
            new StubMediaModule(),
            new RecordingSponsorshipModule([presentingSponsor.Id, prizeSponsor.Id]),
            CompetitionTestSupport.CreateMapper(),
            CompetitionTestSupport.CreateModuleEventPublisher());

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.ReplaceSponsorPlacementsAsync(game.Id, new ReplaceGameSponsorsDTO
        {
            SponsorPlacements =
            [
                new GameSponsorPlacementInputDTO
                {
                    SponsorId = presentingSponsor.Id,
                    Context = SponsorContractContext.TournamentPartner,
                    DisplayOrder = 1
                },
                new GameSponsorPlacementInputDTO
                {
                    SponsorId = prizeSponsor.Id,
                    Context = SponsorContractContext.PrizePartner,
                    DisplayOrder = 2
                }
            ]
        }));

        Assert.Equal("A game can only have one sponsor.", exception.Message);
    }

    [Fact]
    public async Task ReplaceSponsorPlacementsAsync_ThrowsWhenSponsorDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Set<Game>().Add(CreateGame());
        await dbContext.SaveChangesAsync();

        var service = new GameService(
            new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext),
            new StubMatchModeratorFactory(),
            new StubMediaModule(),
            new RecordingSponsorshipModule([]),
            CompetitionTestSupport.CreateMapper(),
            CompetitionTestSupport.CreateModuleEventPublisher());
        var gameId = await dbContext.Set<Game>().Select(game => game.Id).SingleAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.ReplaceSponsorPlacementsAsync(gameId, new ReplaceGameSponsorsDTO
        {
            SponsorPlacements =
            [
                new GameSponsorPlacementInputDTO
                {
                    SponsorId = 404,
                    Context = SponsorContractContext.TournamentPartner,
                    DisplayOrder = 1
                }
            ]
        }));

        Assert.Equal("Sponsor with ID 404 not found", exception.Message);
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static IFormFile CreateFormFile()
    {
        var bytes = Encoding.UTF8.GetBytes("logo");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "logo", "logo.png");
    }

    private static Game CreateGame()
    {
        return new Game("Counter-Strike 2", BracketType.SingleElimination, GameFormat.BestOf3, GameFormat.BestOf5, ParticipationMode.Team, 5)
        {
            Id = Guid.NewGuid()
        };
    }

    private static Sponsor CreateSponsor(int id, string name, SponsorTier tier)
    {
        return new Sponsor
        {
            Id = id,
            Name = name,
            SponsorTier = tier,
            LogoUrl = $"/images/{name.ToLowerInvariant().Replace(' ', '-')}.png",
            InfoUrl = $"https://example.test/{name.ToLowerInvariant().Replace(' ', '-')}",
            Description = $"{name} description"
        };
    }

    private static SponsorPlacementSummary CreateSponsorPlacementSummary(
        Guid gameId,
        Sponsor sponsor,
        SponsorContractContext context,
        string? headline,
        string? supportLine,
        int displayOrder)
    {
        return new SponsorPlacementSummary(
            new SponsorPlacementId(1),
            new GameId(gameId),
            new SponsorSummary(
                new SponsorId(sponsor.Id),
                sponsor.Name,
                (SponsorContractTier)sponsor.SponsorTier,
                sponsor.LogoUrl,
                sponsor.InfoUrl,
                sponsor.Description),
            context,
            headline,
            supportLine,
            displayOrder);
    }

    private sealed class StubMediaModule : IMediaModule
    {
        public Task<StoredMediaAsset> SaveImageAsync(MediaUpload upload, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StoredMediaAsset("/images/mock-upload.png"));
        }

        public Task DeleteImageAsync(string? imageUrl, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubFileService : IFileService
    {
        public Task<string> SaveImageAsync(IFormFile image, CancellationToken cancellationToken = default)
            => Task.FromResult("/images/mock-upload.png");

        public Task DeleteImageAsync(string? imageUrl)
            => Task.CompletedTask;
    }

    private sealed class StubMatchModeratorFactory : IMatchModeratorFactory
    {
        public IMatchModerator GetMatchModerator(BracketType bracketType)
        {
            return new StubMatchModerator();
        }
    }

    private sealed class StubMatchModerator : IMatchModerator
    {
        public IEnumerable<Match> GenerateMatchesForGame(Game game)
        {
            return [];
        }

        public void DeterminePlacements(Game game)
        {
        }
    }

    private sealed class RecordingSponsorshipModule(
        IReadOnlyCollection<int> knownSponsorIds,
        SponsorPlacementSummary? currentPlacement = null) : ISponsorshipModule
    {
        private SponsorPlacementSummary? _currentPlacement = currentPlacement;

        public SponsorPlacementInput? ReplacedPlacement { get; private set; }

        public Task<SponsorSummary?> GetSponsorSummaryAsync(SponsorId sponsorId, CancellationToken cancellationToken = default)
            => Task.FromResult<SponsorSummary?>(null);

        public Task<IReadOnlyList<SponsorSummary>> GetSponsorsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SponsorSummary>>([]);

        public Task<IReadOnlyDictionary<GameId, SponsorPlacementSummary>> GetSponsorPlacementsAsync(
            IReadOnlyCollection<GameId> gameIds,
            CancellationToken cancellationToken = default)
        {
            if (_currentPlacement is null)
                return Task.FromResult<IReadOnlyDictionary<GameId, SponsorPlacementSummary>>(new Dictionary<GameId, SponsorPlacementSummary>());

            return Task.FromResult<IReadOnlyDictionary<GameId, SponsorPlacementSummary>>(
                new Dictionary<GameId, SponsorPlacementSummary> { [_currentPlacement.GameId] = _currentPlacement });
        }

        public Task<SponsorPlacementSummary?> GetSponsorPlacementAsync(GameId gameId, CancellationToken cancellationToken = default)
            => Task.FromResult(_currentPlacement?.GameId == gameId ? _currentPlacement : null);

        public Task ReplaceSponsorPlacementAsync(GameId gameId, SponsorPlacementInput? placement, CancellationToken cancellationToken = default)
        {
            if (placement is not null && !knownSponsorIds.Contains(placement.SponsorId.Value))
                throw new NotFoundException($"Sponsor with ID {placement.SponsorId.Value} not found");

            ReplacedPlacement = placement;
            _currentPlacement = placement is null
                ? null
                : new SponsorPlacementSummary(
                    new SponsorPlacementId(_currentPlacement?.Id.Value ?? 1),
                    gameId,
                    _currentPlacement?.Sponsor ?? new SponsorSummary(
                        placement.SponsorId,
                        string.Empty,
                        SponsorContractTier.Bronze,
                        string.Empty,
                        string.Empty,
                        string.Empty),
                    placement.Context,
                    placement.Headline,
                    placement.SupportLine,
                    placement.DisplayOrder);
            return Task.CompletedTask;
        }
    }
}
