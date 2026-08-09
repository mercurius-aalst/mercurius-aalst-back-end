using Mercurius.Modules.Competition.Application.DTOs.Games;
using Mercurius.Modules.Competition.Application.DTOs.Placements;
using Mercurius.Modules.Competition.Domain;
using Mercurius.Modules.Competition.Extensions;
using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Media.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Sponsorship.Contracts;
using Microsoft.EntityFrameworkCore;
using Platform.Eventing;
using GameCanceledIntegrationEvent = Mercurius.Modules.Competition.Contracts.GameCanceledIntegrationEvent;
using GameCompletedIntegrationEvent = Mercurius.Modules.Competition.Contracts.GameCompletedIntegrationEvent;
using GameCreatedIntegrationEvent = Mercurius.Modules.Competition.Contracts.GameCreatedIntegrationEvent;
using GameDeletedIntegrationEvent = Mercurius.Modules.Competition.Contracts.GameDeletedIntegrationEvent;
using GameResetIntegrationEvent = Mercurius.Modules.Competition.Contracts.GameResetIntegrationEvent;
using GameStartedIntegrationEvent = Mercurius.Modules.Competition.Contracts.GameStartedIntegrationEvent;
using GameUpdatedIntegrationEvent = Mercurius.Modules.Competition.Contracts.GameUpdatedIntegrationEvent;
using PlacementAssignedIntegrationEvent =
    Mercurius.Modules.Competition.Contracts.PlacementAssignedIntegrationEvent;

namespace Mercurius.Modules.Competition.Application.Services;

internal sealed class GameService : IGameQueries, IGameManagementCommands, IGameLifecycleCommands
{
    private readonly ICompetitionDbContext _dbContext;
    private readonly IMatchModeratorFactory _matchModeratorFactory;
    private readonly IMediaModule _mediaModule;
    private readonly ISponsorshipModule _sponsorshipModule;
    private readonly CompetitionDtoMapper _mapper;
    private readonly IModuleEventPublisher _moduleEventPublisher;

    public GameService(
        ICompetitionDbContext dbContext,
        IMatchModeratorFactory matchModeratorFactory,
        IMediaModule mediaModule,
        ISponsorshipModule sponsorshipModule,
        CompetitionDtoMapper mapper,
        IModuleEventPublisher moduleEventPublisher)
    {
        _dbContext = dbContext;
        _matchModeratorFactory = matchModeratorFactory;
        _mediaModule = mediaModule;
        _sponsorshipModule = sponsorshipModule;
        _mapper = mapper;
        _moduleEventPublisher = moduleEventPublisher;
    }

    public async Task<GetGameDTO> CreateGameAsync(
        CreateGameDTO createGameDTO,
        CancellationToken cancellationToken = default)
    {
        if (await GameNameExistsAsync(createGameDTO.Name, cancellationToken))
            throw new ValidationException($"Game {createGameDTO.Name} already created");
        if (createGameDTO.Image is null)
            throw new ValidationException("A game banner/ image is required.");

        var game = new Game(
            createGameDTO.Name,
            (BracketType)createGameDTO.BracketType,
            (GameFormat)createGameDTO.Format,
            (GameFormat)createGameDTO.FinalsFormat,
            (ParticipationMode)createGameDTO.ParticipationMode!.Value,
            createGameDTO.TeamSize,
            createGameDTO.PlannedStartTime.EnsureUtc(),
            createGameDTO.AverageGameDurationMinutes,
            createGameDTO.RoundBreakDurationMinutes);

        await using var imageStream = createGameDTO.Image.OpenReadStream();
        var asset = await _mediaModule.SaveImageAsync(
            new MediaUpload(
                imageStream,
                createGameDTO.Image.FileName,
                createGameDTO.Image.ContentType,
                createGameDTO.Image.Length),
            cancellationToken);
        game.ImageUrl = asset.Url;

        _dbContext.Games.Add(game);
        _moduleEventPublisher.Publish(new GameCreatedIntegrationEvent(new GameId(game.Id), game.Name));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetGameByIdAsync(game.Id, cancellationToken);
    }

    public async Task<GetGameDTO> GetGameByIdAsync(
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var game = await CreateGameDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(game => game.Id == gameId, cancellationToken);
        if (game is null)
            throw new NotFoundException($"{nameof(Game)} not found");

        return await _mapper.ToGetGameDtoAsync(game, cancellationToken);
    }

    public async Task<IEnumerable<GetGameDTO>> GetAllGamesAsync(
        CancellationToken cancellationToken = default)
    {
        var games = await CreateGameListQuery()
            .AsNoTracking()
            .OrderBy(game => game.PlannedStartTime)
            .ThenBy(game => game.Name)
            .ThenBy(game => game.Id)
            .ToListAsync(cancellationToken);

        return await _mapper.ToGetGameDtosAsync(games, cancellationToken);
    }

    public async Task<GetGameDTO> UpdateGameAsync(
        Guid id,
        UpdateGameDTO gameDTO,
        CancellationToken cancellationToken = default)
    {
        var game = await GetGameForMutationAsync(id, cancellationToken);
        if (game.Name != gameDTO.Name && await GameNameExistsAsync(gameDTO.Name, cancellationToken))
            throw new ValidationException($"Game {gameDTO.Name} already exists");

        game.Update(
            gameDTO.Name,
            (BracketType)gameDTO.BracketType,
            (GameFormat)gameDTO.Format,
            (GameFormat)gameDTO.FinalsFormat,
            (ParticipationMode)gameDTO.ParticipationMode!.Value,
            gameDTO.TeamSize,
            gameDTO.PlannedStartTime.EnsureUtc(),
            gameDTO.AverageGameDurationMinutes,
            gameDTO.RoundBreakDurationMinutes);

        if (gameDTO.Image is not null)
        {
            await using var imageStream = gameDTO.Image.OpenReadStream();
            var asset = await _mediaModule.SaveImageAsync(
                new MediaUpload(
                    imageStream,
                    gameDTO.Image.FileName,
                    gameDTO.Image.ContentType,
                    gameDTO.Image.Length),
                cancellationToken);
            game.ImageUrl = asset.Url;
        }

        _moduleEventPublisher.Publish(new GameUpdatedIntegrationEvent(new GameId(game.Id), game.Name));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetGameByIdAsync(game.Id, cancellationToken);
    }

    public async Task DeleteGameAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await GetGameForSimpleMutationAsync(id, cancellationToken);
        if (game.Status == GameStatus.InProgress)
            throw new ValidationException("Game cannot be deleted when already in progress.");

        _dbContext.Games.Remove(game);
        _moduleEventPublisher.Publish(new GameDeletedIntegrationEvent(new GameId(game.Id)));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelGameAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await GetGameForSimpleMutationAsync(id, cancellationToken);
        game.Cancel();
        _moduleEventPublisher.Publish(new GameCanceledIntegrationEvent(new GameId(game.Id), game.Name));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task StartGameAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await GetGameForMutationAsync(id, cancellationToken);
        game.Start();
        var matchModerator = _matchModeratorFactory.GetMatchModerator(game.BracketType);
        game.Matches = matchModerator.GenerateMatchesForGame(game).ToList();
        AssignEstimatedSchedule(game);
        _moduleEventPublisher.Publish(new GameStartedIntegrationEvent(new GameId(game.Id), game.StartTime));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<GetPlacementDTO>> CompleteGameAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var game = await GetGameForMutationAsync(id, cancellationToken);
        game.Complete();
        var matchModerator = _matchModeratorFactory.GetMatchModerator(game.BracketType);
        matchModerator.DeterminePlacements(game);
        _moduleEventPublisher.Publish(new GameCompletedIntegrationEvent(new GameId(game.Id), game.EndTime));
        foreach (var placement in game.Placements)
        {
            foreach (var participantId in placement.Users.Select(user => user.UserId)
                         .Concat(placement.Teams.Select(team => team.TeamId)))
            {
                _moduleEventPublisher.Publish(new PlacementAssignedIntegrationEvent(
                    new GameId(game.Id),
                    placement.Place,
                    participantId));
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        var mapped = await _mapper.ToGetGameDtoAsync(game, cancellationToken);
        return mapped.Placements;
    }

    public async Task ResetGameAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await GetGameForMutationAsync(id, cancellationToken);
        game.Reset();
        _moduleEventPublisher.Publish(new GameResetIntegrationEvent(new GameId(game.Id)));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GetGameDTO> ReplaceSponsorPlacementsAsync(
        Guid id,
        ReplaceGameSponsorsDTO sponsorDTO,
        CancellationToken cancellationToken = default)
    {
        _ = await GetGameForSimpleMutationAsync(id, cancellationToken);
        var placements = sponsorDTO.SponsorPlacements ?? [];
        if (placements.Count > 1)
            throw new ValidationException("A game can only have one sponsor.");

        var placement = placements.SingleOrDefault();
        await _sponsorshipModule.ReplaceSponsorPlacementAsync(
            new GameId(id),
            placement is null
                ? null
                : new SponsorPlacementInput(
                    new SponsorId(placement.SponsorId),
                    placement.Context,
                    placement.Headline,
                    placement.SupportLine,
                    placement.DisplayOrder),
            cancellationToken);

        return await GetGameByIdAsync(id, cancellationToken);
    }

    private Task<bool> GameNameExistsAsync(string name, CancellationToken cancellationToken) =>
        _dbContext.Games.AnyAsync(game => game.Name == name, cancellationToken);

    private async Task<Game> GetGameForMutationAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await CreateGameDetailsQuery()
            .FirstOrDefaultAsync(game => game.Id == gameId, cancellationToken);
        if (game is null)
            throw new NotFoundException($"{nameof(Game)} not found");
        return game;
    }

    private async Task<Game> GetGameForSimpleMutationAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await _dbContext.Games.FirstOrDefaultAsync(game => game.Id == gameId, cancellationToken);
        if (game is null)
            throw new NotFoundException($"{nameof(Game)} not found");
        return game;
    }

    private IQueryable<Game> CreateGameDetailsQuery()
    {
        return _dbContext.Games
            .AsSplitQuery()
            .Include(game => game.TournamentRegistrations)
                .ThenInclude(registration => registration.RosterMembers)
            .Include(game => game.Matches)
            .Include(game => game.Placements)
                .ThenInclude(placement => placement.Users)
            .Include(game => game.Placements)
                .ThenInclude(placement => placement.Teams);
    }

    private IQueryable<Game> CreateGameListQuery()
    {
        return _dbContext.Games;
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
        foreach (var round in game.Matches
                     .GroupBy(match => match.RoundNumber)
                     .OrderBy(group => group.Key))
        {
            var roundDuration = TimeSpan.Zero;
            foreach (var match in round.OrderBy(match => match.MatchNumber))
            {
                var matchDuration = TimeSpan.FromMinutes(
                    game.AverageGameDurationMinutes * GetDurationMultiplier(match.Format));
                var estimatedEnd = AddScheduleTime(currentRoundStart, matchDuration);
                match.SetEstimatedWindow(currentRoundStart, estimatedEnd);
                if (matchDuration > roundDuration)
                    roundDuration = matchDuration;
                if (!latestEnd.HasValue || estimatedEnd > latestEnd.Value)
                    latestEnd = estimatedEnd;
            }

            currentRoundStart = AddScheduleTime(
                AddScheduleTime(currentRoundStart, roundDuration),
                TimeSpan.FromMinutes(game.RoundBreakDurationMinutes));
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

    private static DateTime AddScheduleTime(DateTime timestamp, TimeSpan duration)
    {
        try
        {
            return timestamp.Add(duration);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ValidationException("Estimated tournament schedule exceeds supported date range.");
        }
    }
}
