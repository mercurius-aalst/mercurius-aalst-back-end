using MercuriusAPI.Data;
using MercuriusAPI.DTOs.LAN.GameDTOs;
using MercuriusAPI.DTOs.LAN.PlacementDTOs;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.LAN.MatchServices;
using Microsoft.EntityFrameworkCore;

namespace MercuriusAPI.Services.LAN.GameServices
{
    public class GameService : IGameService
    {
        private readonly MercuriusDBContext _dbContext;
        private readonly IMatchModeratorFactory _matchGeneratorFactory;

        public GameService(MercuriusDBContext dbContext, IMatchModeratorFactory matchGeneratorFactory)
        {
            _dbContext = dbContext;
            _matchGeneratorFactory = matchGeneratorFactory;
        }

        public async Task<GetGameDTO> CreateGameAsync(CreateGameDTO createGameDTO)
        {
            if(await CheckIfGameNameExistsAsync(createGameDTO.Name))
                throw new ValidationException($"Game {createGameDTO.Name} already created");
            var game = new Game(createGameDTO.Name, createGameDTO.BracketType, createGameDTO.Format, createGameDTO.FinalsFormat, createGameDTO.ParticipantType);
            _dbContext.Games.Add(game);
            await _dbContext.SaveChangesAsync();
            return new GetGameDTO(game);
        }
        public async Task<Game> GetGameByIdAsync(int gameId)
        {
            var game = await _dbContext.Games.Include(g => g.Participants).Include(g => g.Matches).FirstOrDefaultAsync(g => g.Id == gameId);
            if(game is null)
                throw new NotFoundException($"{nameof(Game)} not found");
            return game;
        }

        public IEnumerable<GetGameDTO> GetAllGames()
        {           
            return _dbContext.Games.Include(g => g.Participants).Include(g => g.Matches).ToList().Select(g => new GetGameDTO(g));
        }

        public async Task<GetGameDTO> UpdateGameAsync(int id, UpdateGameDTO gameDTO)
        {
            var game = await GetGameByIdAsync(id);
            game.Update(gameDTO.Name, gameDTO.BracketType, gameDTO.Format, gameDTO.FinalsFormat);
            _dbContext.Games.Update(game);
            await _dbContext.SaveChangesAsync();
            return new GetGameDTO(game);
        }

        public async Task DeleteGameAsync(int id)
        {
            var game = await GetGameByIdAsync(id);
            if(game.Status == GameStatus.InProgress)
                throw new ValidationException("Game cannot be deleted when already in progress.");
            _dbContext.Games.Remove(game);
            await _dbContext.SaveChangesAsync();
        }

        public async Task CancelGameAsync(int id)
        {
            var game = await GetGameByIdAsync(id);
            game.Cancel();
            _dbContext.Games.Update(game);
            await _dbContext.SaveChangesAsync();
        }

        public async Task StartGameAsync(int id)
        {
            var game = await GetGameByIdAsync(id);
            game.Start();
            var matchGenerator = _matchGeneratorFactory.GetMatchModerator(game.BracketType);
            game.Matches = matchGenerator.GenerateMatchesForGame(game).ToList();

            _dbContext.Games.Update(game);           
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<GetPlacementDTO>> CompleteGameAsync(int id)
        {
            var game = await GetGameByIdAsync(id);
            game.Complete();

            var matchModerator = _matchGeneratorFactory.GetMatchModerator(game.BracketType);
            matchModerator.DeterminePlacements(game);

            _dbContext.Games.Update(game);
            await _dbContext.SaveChangesAsync();
            return game.Placements.Select(p => new GetPlacementDTO(p, game.ParticipantType));
        }

        public async Task ResetGameAsync(int id)
        {
            var game = await GetGameByIdAsync(id);
            game.Reset();
            _dbContext.Games.Update(game);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<GetGameDTO> AddParticipantAsync(int id, Participant participant)
        {
            var game = await GetGameByIdAsync(id);
            var expectedType = game.ParticipantType switch
            {
                ParticipantType.Player => typeof(Player),
                ParticipantType.Team => typeof(Team),
                _ => typeof(Participant)
            };
            if(participant.GetType() != expectedType)
                throw new ValidationException($"This game only accepts {nameof(expectedType)}s as participants.");
            if(game.Status != GameStatus.Scheduled)
                throw new ValidationException("Game must be scheduled for registrations.");
            game.Participants.Add(participant);
            _dbContext.Games.Update(game);
            await _dbContext.SaveChangesAsync();
            return new GetGameDTO(game);
        }

        public async Task<GetGameDTO> RemoveParticipantAsync(int id, Participant participant)
        {
            var game = await GetGameByIdAsync(id);
            if(game.Status != GameStatus.Scheduled)
                throw new ValidationException("Game must be scheduled for participant changes");
            if(!game.Participants.Any(p => p.Id == participant.Id))
                throw new NotFoundException($"{nameof(Participant)} not found for game {game.Name}");
            game.Participants.Remove(participant);
            _dbContext.Games.Update(game);
            await _dbContext.SaveChangesAsync();
            return new GetGameDTO(game);
        }

        private async Task<bool> CheckIfGameNameExistsAsync(string name)
        {
            return await _dbContext.Games.AnyAsync(g => g.Name == name);
        }
    }
}

