using MercuriusAPI.Data;
using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.Images;
using Microsoft.EntityFrameworkCore;

namespace MercuriusAPI.Services.LAN.PlayerServices
{
    public class PlayerService : IPlayerService
    {
        private readonly MercuriusDBContext _dbContext;
        private readonly IImageService _imageService;

        public PlayerService(MercuriusDBContext dbContext, IImageService imageService)
        {
            _dbContext = dbContext;
            _imageService = imageService;
        }
        public async Task<Player> GetPlayerByIdAsync(int playerId)
        {
            var player = await _dbContext.Players.FindAsync(playerId);
            if(player is null)
                throw new NotFoundException($"{nameof(Player)} not found");
            return player;
        }

        public IEnumerable<GetPlayerDTO> GetAllPlayers()
        {
            return _dbContext.Players.Select(p => new GetPlayerDTO(p));
        }

        public async Task<GetPlayerDTO> CreatePlayerAsync(CreatePlayerDTO playerDTO)
        {
            if(await CheckUsernameExists(playerDTO.Username) || await CheckEmailExistsAsync(playerDTO.Email))
                throw new ValidationException("Username or email already exists");

            string pictureUrl = string.Empty;
            if(playerDTO.Picture is null)
                pictureUrl = "default player-picture url"; // Placeholder for default picture URL
            else
                pictureUrl = await _imageService.UploadFileAsync(playerDTO.Picture);

            var player = new Player(playerDTO.Username, playerDTO.Firstname, playerDTO.Lastname, playerDTO.Email, pictureUrl, playerDTO.DiscordId, playerDTO.SteamId, playerDTO.RiotId);
            _dbContext.Players.Add(player);
            await _dbContext.SaveChangesAsync();
            return new GetPlayerDTO(player);
        }
        public async Task<GetPlayerDTO> UpdatePlayerAsync(int id, UpdatePlayerDTO playerDTO)
        {
            var player = await _dbContext.Players.FindAsync(id);
            if(player is null)
                throw new NotFoundException($"{nameof(Player)} not found");
            if(player.Username != playerDTO.Username && await CheckUsernameExists(playerDTO.Username))
                throw new ValidationException("New username already exists");
            
            string oldPictureUrl = player.PictureUrl;
            string pictureUrl = oldPictureUrl;
            if(playerDTO.Picture is not null)
                pictureUrl = await _imageService.UploadFileAsync(playerDTO.Picture);             
            
            player.Update(playerDTO.Firstname, playerDTO.Lastname, playerDTO.Username, pictureUrl, playerDTO.DiscordId, playerDTO.SteamId, playerDTO.RiotId);
            _dbContext.Players.Update(player);
            await _dbContext.SaveChangesAsync();

            await _imageService.DeleteFileAsync(oldPictureUrl);

            return new GetPlayerDTO(player);
        }
        public async Task DeletePlayerAsync(int playerId)
        {
            var player = await _dbContext.Players.FindAsync(playerId);
            if(player is null)
                throw new NotFoundException($"{nameof(Player)} not found");
            _dbContext.Players.Remove(player);
            await _dbContext.SaveChangesAsync();
        }


        private Task<bool> CheckUsernameExists(string username)
        {
            return _dbContext.Players.AnyAsync(p => p.Username.Equals(username));
        }
        private Task<bool> CheckEmailExistsAsync(string email)
        {
            return _dbContext.Players.AnyAsync(p => p.Email.Equals(email));
        }
    }
}
