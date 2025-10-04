using MercuriusAPI.Data;
using MercuriusAPI.DTOs.LAN.GameDTOs;
using MercuriusAPI.DTOs.LAN.PlacementDTOs;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.Files;
using MercuriusAPI.Services.LAN.MatchServices;
using Microsoft.EntityFrameworkCore;

namespace MercuriusAPI.Services.LAN.GameServices
{
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
            if(await CheckIfGameNameExistsInCurrentSeasonAsync(createGameDTO.Name))
                throw new ValidationException($"Game {createGameDTO.Name} already created");
            if(createGameDTO.Image == null)
                throw new ValidationException("A game banner/ image is required.");

            var game = new Game(createGameDTO.Name, createGameDTO.BracketType, createGameDTO.Format, createGameDTO.FinalsFormat, createGameDTO.ParticipantType, createGameDTO.RegisterFormUrl);

            var bannerPath = await _fileService.SaveImageAsync(createGameDTO.Image);
            game.ImageUrl = bannerPath;


            _dbContext.Games.Add(game);
            await _dbContext.SaveChangesAsync();
            return new GetGameDTO(game);
        }
        public async Task<Game> GetGameByIdAsync(int gameId)
        {
            var game = await _dbContext.Games.Include(g => g.Participants).Include(g => g.Matches).Include(g => g.Placements).ThenInclude(p => p.Participants).FirstOrDefaultAsync(g => g.Id == gameId);
            if(game is null)
                throw new NotFoundException($"{nameof(Game)} not found");
            return game;
        }

        public IEnumerable<GetGameDTO> GetAllGames(string? academicSeason)
        {
            if(string.IsNullOrEmpty(academicSeason))
                academicSeason = AcademicSeasonHelper.GetCurrent();
            return _dbContext.Games.Where(g => g.AcademicSeason == academicSeason).Include(g => g.Participants).Include(g => g.Matches).ToList().Select(g => new GetGameDTO(g));
        }

        public async Task<GetGameDTO> UpdateGameAsync(int id, UpdateGameDTO gameDTO)
        {
            var game = await GetGameByIdAsync(id);
            if(game.Name != gameDTO.Name && await CheckIfGameNameExistsInCurrentSeasonAsync(gameDTO.Name))
                throw new ValidationException($"Game {gameDTO.Name} already exists");

            game.Update(gameDTO.Name, gameDTO.BracketType, gameDTO.Format, gameDTO.FinalsFormat, gameDTO.RegisterFormUrl);

            if(gameDTO.Image != null)
            {
                var bannerPath = await _fileService.SaveImageAsync(gameDTO.Image);
                game.ImageUrl = bannerPath;
            }

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
            game.AddParticipant(participant);
            _dbContext.Games.Update(game);
            await _dbContext.SaveChangesAsync();
            return new GetGameDTO(game);
        }

        public async Task<GetGameDTO> RemoveParticipantAsync(int id, Participant participant)
        {
            var game = await GetGameByIdAsync(id);
            game.RemoveParticipant(participant);
            _dbContext.Games.Update(game);
            await _dbContext.SaveChangesAsync();
            return new GetGameDTO(game);
        }

        private async Task<bool> CheckIfGameNameExistsInCurrentSeasonAsync(string name)
        {
            return await _dbContext.Games.AnyAsync(g => g.Name == name && g.AcademicSeason == AcademicSeasonHelper.GetCurrent());
        }
    }
}

