using System.Text;
using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.SponsorDTOs;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.Files;
using Mercurius.LAN.API.Services.GameServices;
using Mercurius.LAN.API.Services.MatchServices;
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
        game.SponsorPlacement = new GameSponsorPlacement
        {
            SponsorId = presentingSponsor.Id,
            Context = SponsorContext.CateringPartner,
            DisplayOrder = 99
        };

        dbContext.Games.Add(game);
        dbContext.Sponsors.Add(presentingSponsor);
        await dbContext.SaveChangesAsync();

        var service = new GameService(dbContext, new StubMatchModeratorFactory(), new StubFileService());
        var updatedGame = await service.ReplaceSponsorPlacementsAsync(game.Id, new ReplaceGameSponsorsDTO
        {
            SponsorPlacements =
            [
                new GameSponsorPlacementInputDTO
                {
                    SponsorId = presentingSponsor.Id,
                    Context = SponsorContext.TournamentPartner,
                    Headline = "Presented by Mercurius Tech",
                    SupportLine = "Main stage and stream support",
                    DisplayOrder = 1
                }
            ]
        });

        Assert.NotNull(updatedGame.SponsorPlacement);
        Assert.Equal(SponsorContext.TournamentPartner, updatedGame.SponsorPlacement.Context);
        Assert.Equal("Mercurius Tech", updatedGame.SponsorPlacement.SponsorName);
        Assert.Equal(SponsorTier.Presenting, updatedGame.SponsorPlacement.SponsorTier);
        Assert.Equal("Presented by Mercurius Tech", updatedGame.SponsorPlacement.Headline);

        var storedPlacements = await dbContext.GameSponsorPlacements
            .OrderBy(placement => placement.DisplayOrder)
            .ToListAsync();
        Assert.Single(storedPlacements);
        Assert.DoesNotContain(storedPlacements, placement => placement.Context == SponsorContext.CateringPartner);
    }

    [Fact]
    public async Task ReplaceSponsorPlacementsAsync_ThrowsWhenMoreThanOneSponsorIsProvided()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateGame();
        var presentingSponsor = CreateSponsor(1, "Mercurius Tech", SponsorTier.Presenting);
        var prizeSponsor = CreateSponsor(2, "Campus Fiber", SponsorTier.Gold);
        dbContext.Games.Add(game);
        dbContext.Sponsors.AddRange(presentingSponsor, prizeSponsor);
        await dbContext.SaveChangesAsync();

        var service = new GameService(dbContext, new StubMatchModeratorFactory(), new StubFileService());

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.ReplaceSponsorPlacementsAsync(game.Id, new ReplaceGameSponsorsDTO
        {
            SponsorPlacements =
            [
                new GameSponsorPlacementInputDTO
                {
                    SponsorId = presentingSponsor.Id,
                    Context = SponsorContext.TournamentPartner,
                    DisplayOrder = 1
                },
                new GameSponsorPlacementInputDTO
                {
                    SponsorId = prizeSponsor.Id,
                    Context = SponsorContext.PrizePartner,
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
        dbContext.Games.Add(CreateGame());
        await dbContext.SaveChangesAsync();

        var service = new GameService(dbContext, new StubMatchModeratorFactory(), new StubFileService());
        var gameId = await dbContext.Games.Select(game => game.Id).SingleAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.ReplaceSponsorPlacementsAsync(gameId, new ReplaceGameSponsorsDTO
        {
            SponsorPlacements =
            [
                new GameSponsorPlacementInputDTO
                {
                    SponsorId = 404,
                    Context = SponsorContext.TournamentPartner,
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
        return new Game("Counter-Strike 2", BracketType.SingleElimination, GameFormat.BestOf3, GameFormat.BestOf5, ParticipationMode.Team, "https://example.test/register")
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

    private sealed class StubFileService : IFileService
    {
        public Task<string> SaveImageAsync(IFormFile image)
        {
            return Task.FromResult("/images/mock-upload.png");
        }
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
}
