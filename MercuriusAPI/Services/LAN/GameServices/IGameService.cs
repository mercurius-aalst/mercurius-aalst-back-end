using MercuriusAPI.DTOs.LAN.GameDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.GameServices
{
    public interface IGameService
    {
        Task<GetGameDTO> AddParticipantAsync(int id, Participant participant);
        void AssignParticipantsToNextMatch(Match match, Game game);
        Task CancelGameAsync(int id);
        Task CompleteGameAsync(int id);
        Task<GetGameDTO> CreateGameAsync(CreateGameDTO createGameDTO);
        Task DeleteGameAsync(int id);
        IEnumerable<GetGameDTO> GetAllGames();
        Task<Game> GetGameByIdAsync(int gameId);
        Task<GetGameDTO> RemoveParticipantAsync(int id, Participant participant);
        Task ResetGameAsync(int id);
        Task StartGameAsync(int id);
        Task<GetGameDTO> UpdateGameAsync(int id, UpdateGameDTO gameDTO);
    }
}