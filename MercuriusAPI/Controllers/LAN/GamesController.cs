using MercuriusAPI.DTOs.LAN.GameDTOs;
using MercuriusAPI.Services.LAN.GameServices;
using MercuriusAPI.Services.LAN.ParticipantServices;
using Microsoft.AspNetCore.Mvc;

namespace MercuriusAPI.Controllers.LAN
{
    [Route("[controller]")]
    [ApiController]
    public class GamesController(IGameService _gameService, IParticipantService _participantService) : ControllerBase
    {

        [HttpGet]
        public IEnumerable<GetGameDTO> GetGames()
        {
            return _gameService.GetAllGames();
        }

        [HttpGet("{id}")]
        public async Task<GetGameDTO> GetGameAsync(int id)
        {
            return new GetGameDTO(await _gameService.GetGameByIdAsync(id));
        }

        [HttpPost]
        public Task<GetGameDTO> CreateGameAsync(CreateGameDTO createGameDTO)
        {
            return _gameService.CreateGameAsync(createGameDTO);
        }

        [HttpPut("{id}")]
        public Task<GetGameDTO> UpdateGameAsync(int id, UpdateGameDTO updateGameDTO)
        {
            return _gameService.UpdateGameAsync(id, updateGameDTO);
        }

        [HttpDelete("{id}")]
        public Task DeleteGameAsync(int id)
        {
            return _gameService.DeleteGameAsync(id);
        }

        [HttpPost("{id}/participants/{participantId}")]
        public async Task<GetGameDTO> RegisterForGameAsync(int id, int participantId)
        {
            var participant = await _participantService.GetParticipantByIdAsync(participantId); 
            return await _gameService.AddParticipantAsync(id, participant);
        }

        [HttpDelete("{id}/participants/{participantId}")]
        public async Task<GetGameDTO> UnregisterFromGameAsync(int id, int participantId)
        {
           var participant = await _participantService.GetParticipantByIdAsync(participantId);
           return await _gameService.RemoveParticipantAsync(id, participant);
        }

        [HttpPost("{id}/start")]
        public Task StartGameAsync(int id)
        {
            return _gameService.StartGameAsync(id);
        }

        [HttpPost("{id}/reset")]
        public Task ResetGameAsync(int id)
        {
            return _gameService.ResetGameAsync(id);
        }

        [HttpPost("{id}/complete")]
        public Task CompleteGameAsync(int id)
        {
            return _gameService.CompleteGameAsync(id);
        }

        [HttpPost("{id}/cancel")]
        public Task CancelGameAsync(int id)
        {
            return _gameService.CancelGameAsync(id);
        }
    }
}
