using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.Files;
using Mercurius.LAN.API.Services.MatchServices;
using Mercurius.Shared.Exceptions;
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

        var game = new Game(createGameDTO.Name, createGameDTO.BracketType, createGameDTO.Format, createGameDTO.FinalsFormat, createGameDTO.ParticipationMode!.Value, createGameDTO.RegisterFormUrl);

        var bannerPath = await _fileService.SaveImageAsync(createGameDTO.Image);
        game.ImageUrl = bannerPath;


        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();
        return new GetGameDTO(game);
    }
    public async Task<Game> GetGameByIdAsync(Guid gameId)
    {
        var game = await _dbContext.Games
            .Include(g => g.RegisteredUsers)
            .Include(g => g.RegisteredTeams)
            .Include(g => g.Matches)
            .Include(g => g.Placements)
                .ThenInclude(p => p.Users)
            .Include(g => g.Placements)
                .ThenInclude(p => p.Teams)
            .FirstOrDefaultAsync(g => g.Id == gameId);
        if (game is null)
            throw new NotFoundException($"{nameof(Game)} not found");
        return game;
    }

    public IEnumerable<GetGameDTO> GetAllGames()
    {
        return _dbContext.Games
            .Include(g => g.RegisteredUsers)
            .Include(g => g.RegisteredTeams)
            .Include(g => g.Matches)
            .ToList()
            .Select(g => new GetGameDTO(g));
    }

    public async Task<GetGameDTO> UpdateGameAsync(Guid id, UpdateGameDTO gameDTO)
    {
        var game = await GetGameByIdAsync(id);
        if (game.Name != gameDTO.Name && await CheckIfGameNameExistsInCurrentSeasonAsync(gameDTO.Name))
            throw new ValidationException($"Game {gameDTO.Name} already exists");

        game.Update(gameDTO.Name, gameDTO.BracketType, gameDTO.Format, gameDTO.FinalsFormat, gameDTO.ParticipationMode!.Value, gameDTO.RegisterFormUrl);

        if (gameDTO.Image != null)
        {
            var bannerPath = await _fileService.SaveImageAsync(gameDTO.Image);
            game.ImageUrl = bannerPath;
        }

        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
        return new GetGameDTO(game);
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
        return new GetGameDTO(game);
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
        return new GetGameDTO(game);
    }

    public async Task<GetGameDTO> UnregisterUserAsync(Guid id, Guid userId)
    {
        var game = await GetGameByIdAsync(id);
        if (game.ParticipationMode != ParticipationMode.Individual)
            throw new ValidationException("Users can only be removed from individual-mode games.");
        game.RemoveUser(userId);
        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
        return new GetGameDTO(game);
    }

    public async Task<GetGameDTO> UnregisterTeamAsync(Guid id, Guid teamId)
    {
        var game = await GetGameByIdAsync(id);
        if (game.ParticipationMode != ParticipationMode.Team)
            throw new ValidationException("Teams can only be removed from team-mode games.");
        game.RemoveTeam(teamId);
        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
        return new GetGameDTO(game);
    }

    private async Task<bool> CheckIfGameNameExistsInCurrentSeasonAsync(string name)
    {
        return await _dbContext.Games.AnyAsync(g => g.Name == name);
    }
}


