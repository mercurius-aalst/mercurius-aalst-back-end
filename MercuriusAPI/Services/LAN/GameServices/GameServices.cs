using MercuriusAPI.Data;
using MercuriusAPI.DTOs.LAN.GameDTOs;
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
                throw new Exception("Game already created");
            var game = new Game(createGameDTO.Name, createGameDTO.BracketType, createGameDTO.Format, createGameDTO.FinalsFormat, createGameDTO.ParticipantType);
            _dbContext.Games.Add(game);
            await _dbContext.SaveChangesAsync();
            return new GetGameDTO(game);
        }
        public async Task<Game> GetGameByIdAsync(int gameId)
        {
            var game = await _dbContext.Games.Include(g => g.Participants).Include(g => g.Matches).FirstOrDefaultAsync(g => g.Id == gameId);
            if(game is null)
                throw new Exception("Game not found");
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
                throw new Exception("Game cannot be deleted anymore");
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

        public async Task CompleteGameAsync(int id)
        {
            var game = await GetGameByIdAsync(id);
            game.Complete();
            _dbContext.Games.Update(game);
            await _dbContext.SaveChangesAsync();
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
                throw new Exception($"Wrong Participant type, expected {expectedType}");
            if(game.Status != GameStatus.Scheduled)
                throw new Exception("Game is not in the correct state for registrations");
            game.Participants.Add(participant);
            _dbContext.Games.Update(game);
            await _dbContext.SaveChangesAsync();
            return new GetGameDTO(game);
        }

        public async Task<GetGameDTO> RemoveParticipantAsync(int id, Participant participant)
        {
            var game = await GetGameByIdAsync(id);
            if(game.Status != GameStatus.Scheduled)
                throw new Exception("Game is not in the correct state for participant changes");
            if(!game.Participants.Any(p => p.Id == participant.Id))
                throw new Exception("Participant not found in game");
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

