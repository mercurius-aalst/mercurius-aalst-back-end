using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.Files;
using Mercurius.LAN.API.Services.MatchServices;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Services.GameServices;

public class GameService : IGameService
{
    private readonly MercuriusDBContext _dbContext;
    private readonly IMatchModeratorFactory _matchGeneratorFactory;
    private readonly IFileService _fileService;

    public GameService(MercuriusDBContext dbContext, IMatchModeratorFactory matchGeneratorFactory, IFileService fileService)
    {
        _dbContext = dbContext;
        _matchGeneratorFactory = matchGeneratorFactory;
        _fileService = fileService;
    }

    public async Task<GetGameDTO> CreateGameAsync(CreateGameDTO createGameDTO)
    {
        if (await CheckIfGameNameExistsInCurrentSeasonAsync(createGameDTO.Name))
            throw new ValidationException($"Game {createGameDTO.Name} already created");
        if (createGameDTO.Image == null)
            throw new ValidationException("A game banner/ image is required.");

        var game = new Game(
            createGameDTO.Name,
            createGameDTO.BracketType,
            createGameDTO.Format,
            createGameDTO.FinalsFormat,
            createGameDTO.ParticipationMode!.Value,
            createGameDTO.RegisterFormUrl,
            createGameDTO.PlannedStartTime,
            createGameDTO.AverageGameDurationMinutes,
            createGameDTO.RoundBreakDurationMinutes);

        var bannerPath = await _fileService.SaveImageAsync(createGameDTO.Image);
        game.ImageUrl = bannerPath;


        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();
        return new GetGameDTO(await GetGameByIdAsync(game.Id));
    }
    public async Task<Game> GetGameByIdAsync(Guid gameId)
    {
        var game = await CreateDetailedGameQuery()
            .Include(g => g.RegisteredUsers)
            .Include(g => g.RegisteredTeams)
            .Include(g => g.Matches)
            .Include(g => g.Placements)
                .ThenInclude(p => p.Users)
            .Include(g => g.Placements)
                .ThenInclude(p => p.Teams)
            .Include(g => g.SponsorPlacement)
                .ThenInclude(placement => placement!.Sponsor)
            .FirstOrDefaultAsync(g => g.Id == gameId);
        if (game is null)
            throw new NotFoundException($"{nameof(Game)} not found");
        return game;
    }

    public IEnumerable<GetGameDTO> GetAllGames()
    {
        return CreateDetailedGameQuery()
            .Include(g => g.RegisteredUsers)
            .Include(g => g.RegisteredTeams)
            .Include(g => g.Matches)
            .Include(g => g.SponsorPlacement)
                .ThenInclude(placement => placement!.Sponsor)
            .ToList()
            .Select(g => new GetGameDTO(g));
    }

    public async Task<GetGameDTO> UpdateGameAsync(Guid id, UpdateGameDTO gameDTO)
    {
        var game = await GetGameByIdAsync(id);
        if (game.Name != gameDTO.Name && await CheckIfGameNameExistsInCurrentSeasonAsync(gameDTO.Name))
            throw new ValidationException($"Game {gameDTO.Name} already exists");

        game.Update(
            gameDTO.Name,
            gameDTO.BracketType,
            gameDTO.Format,
            gameDTO.FinalsFormat,
            gameDTO.ParticipationMode!.Value,
            gameDTO.RegisterFormUrl,
            gameDTO.PlannedStartTime,
            gameDTO.AverageGameDurationMinutes,
            gameDTO.RoundBreakDurationMinutes);

        if (gameDTO.Image != null)
        {
            var bannerPath = await _fileService.SaveImageAsync(gameDTO.Image);
            game.ImageUrl = bannerPath;
        }

        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
        return new GetGameDTO(await GetGameByIdAsync(game.Id));
    }

    public async Task DeleteGameAsync(Guid id)
    {
        var game = await GetGameByIdAsync(id);
        if (game.Status == GameStatus.InProgress)
            throw new ValidationException("Game cannot be deleted when already in progress.");
        _dbContext.Games.Remove(game);
        await _dbContext.SaveChangesAsync();
    }

    public async Task CancelGameAsync(Guid id)
    {
        var game = await GetGameByIdAsync(id);
        game.Cancel();
        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
    }

    public async Task StartGameAsync(Guid id)
    {
        var game = await GetGameByIdAsync(id);
        game.Start();
        var matchGenerator = _matchGeneratorFactory.GetMatchModerator(game.BracketType);
        game.Matches = matchGenerator.GenerateMatchesForGame(game).ToList();
        AssignEstimatedSchedule(game);

        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<GetPlacementDTO>> CompleteGameAsync(Guid id)
    {
        var game = await GetGameByIdAsync(id);
        game.Complete();

        var matchModerator = _matchGeneratorFactory.GetMatchModerator(game.BracketType);
        matchModerator.DeterminePlacements(game);

        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
        return game.Placements.Select(p => new GetPlacementDTO(p, game.ParticipationMode));
    }

    public async Task ResetGameAsync(Guid id)
    {
        var game = await GetGameByIdAsync(id);
        game.Reset();
        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<GetGameDTO> RegisterUserAsync(Guid id, Guid userId)
    {
        var game = await GetGameByIdAsync(id);
        if (game.ParticipationMode != ParticipationMode.Individual)
            throw new ValidationException("Users can only register for individual-mode games.");
        var user = await _dbContext.Users.FindAsync(userId);
        if (user is null)
            throw new NotFoundException($"{nameof(User)} not found");
        game.RegisterUser(user);
        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
        return new GetGameDTO(await GetGameByIdAsync(game.Id));
    }

    public async Task<GetGameDTO> RegisterTeamAsync(Guid id, Guid teamId)
    {
        var game = await GetGameByIdAsync(id);
        if (game.ParticipationMode != ParticipationMode.Team)
            throw new ValidationException("Teams can only register for team-mode games.");
        var team = await _dbContext.Teams.FindAsync(teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        game.RegisterTeam(team);
        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
        return new GetGameDTO(await GetGameByIdAsync(game.Id));
    }

    public async Task<GetGameDTO> UnregisterUserAsync(Guid id, Guid userId)
    {
        var game = await GetGameByIdAsync(id);
        if (game.ParticipationMode != ParticipationMode.Individual)
            throw new ValidationException("Users can only be removed from individual-mode games.");
        game.RemoveUser(userId);
        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
        return new GetGameDTO(await GetGameByIdAsync(game.Id));
    }

    public async Task<GetGameDTO> UnregisterTeamAsync(Guid id, Guid teamId)
    {
        var game = await GetGameByIdAsync(id);
        if (game.ParticipationMode != ParticipationMode.Team)
            throw new ValidationException("Teams can only be removed from team-mode games.");
        game.RemoveTeam(teamId);
        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
        return new GetGameDTO(await GetGameByIdAsync(game.Id));
    }

    public async Task<GetGameDTO> ReplaceSponsorPlacementsAsync(Guid id, ReplaceGameSponsorsDTO sponsorDTO)
    {
        var game = await GetGameByIdAsync(id);
        var placements = sponsorDTO.SponsorPlacements ?? [];
        if (placements.Count > 1)
            throw new ValidationException("A game can only have one sponsor.");

        var sponsorIds = placements.Select(placement => placement.SponsorId).Distinct().ToList();
        var sponsorsById = sponsorIds.Count == 0
            ? new Dictionary<int, Sponsor>()
            : await _dbContext.Sponsors
                .Where(sponsor => sponsorIds.Contains(sponsor.Id))
                .ToDictionaryAsync(sponsor => sponsor.Id);

        var missingSponsorIds = sponsorIds.Where(sponsorId => !sponsorsById.ContainsKey(sponsorId)).ToList();
        if (missingSponsorIds.Count != 0)
            throw new NotFoundException($"Sponsor with ID {missingSponsorIds[0]} not found");

        var placement = placements.SingleOrDefault();
        if (placement is null)
        {
            if (game.SponsorPlacement is not null)
                _dbContext.GameSponsorPlacements.Remove(game.SponsorPlacement);

            game.SponsorPlacement = null;
        }
        else if (game.SponsorPlacement is null)
        {
            var gameSponsorPlacement = new GameSponsorPlacement();
            ApplySponsorPlacement(gameSponsorPlacement, placement, game.Id);
            game.SponsorPlacement = gameSponsorPlacement;
            _dbContext.GameSponsorPlacements.Add(gameSponsorPlacement);
        }
        else
        {
            ApplySponsorPlacement(game.SponsorPlacement, placement, game.Id);
        }
        await _dbContext.SaveChangesAsync();

        return new GetGameDTO(await GetGameByIdAsync(game.Id));
    }

    private async Task<bool> CheckIfGameNameExistsInCurrentSeasonAsync(string name)
    {
        return await _dbContext.Games.AnyAsync(g => g.Name == name);
    }

    private IQueryable<Game> CreateDetailedGameQuery()
    {
        return _dbContext.Games
            .Include(g => g.Placements)
            .Include(g => g.SponsorPlacement);
    }

    private static void ApplySponsorPlacement(GameSponsorPlacement gameSponsorPlacement, GameSponsorPlacementInputDTO placement, Guid gameId)
    {
        gameSponsorPlacement.GameId = gameId;
        gameSponsorPlacement.SponsorId = placement.SponsorId;
        gameSponsorPlacement.Context = placement.Context;
        gameSponsorPlacement.Headline = placement.Headline;
        gameSponsorPlacement.SupportLine = placement.SupportLine;
        gameSponsorPlacement.DisplayOrder = placement.DisplayOrder;
    }

    private static void AssignEstimatedSchedule(Game game)
    {
        if (game.Matches.Count == 0)
        {
            game.EstimatedEndTime = null;
            return;
        }

        var currentRoundStart = game.PlannedStartTime;
        DateTime? latestEnd = null;
        var finalRoundNumber = game.Matches.Max(match => match.RoundNumber);

        foreach (var round in game.Matches
                     .GroupBy(match => match.RoundNumber)
                     .OrderBy(group => group.Key))
        {
            var roundDuration = TimeSpan.Zero;
            var orderedMatches = round.OrderBy(match => match.MatchNumber).ToList();

            foreach (var match in orderedMatches)
            {
                var format = match.RoundNumber == finalRoundNumber ? game.FinalsFormat : match.Format;
                var matchDuration = TimeSpan.FromMinutes(game.AverageGameDurationMinutes * GetDurationMultiplier(format));
                var estimatedEnd = currentRoundStart.Add(matchDuration);
                match.SetEstimatedWindow(currentRoundStart, estimatedEnd);

                if (matchDuration > roundDuration)
                    roundDuration = matchDuration;
                if (!latestEnd.HasValue || estimatedEnd > latestEnd.Value)
                    latestEnd = estimatedEnd;
            }

            currentRoundStart = currentRoundStart
                .Add(roundDuration)
                .Add(TimeSpan.FromMinutes(game.RoundBreakDurationMinutes));
        }

        game.EstimatedEndTime = latestEnd;
    }

    private static int GetDurationMultiplier(GameFormat format)
    {
        return format switch
        {
            GameFormat.BestOf1 => 1,
            GameFormat.BestOf3 => 3,
            GameFormat.BestOf5 => 5,
            _ => 1
        };
    }
}


