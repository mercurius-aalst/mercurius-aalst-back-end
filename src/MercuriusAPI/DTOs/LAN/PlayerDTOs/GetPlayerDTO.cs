using MercuriusAPI.DTOs.LAN.ParticipantDTOs;
using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.DTOs.LAN.PlayerDTOs
{
    public class GetPlayerDTO: GetParticipantDTO
    {
        public string Username { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }

        public string? DiscordId { get; set; } = string.Empty;
        public string? SteamId { get; set; } = string.Empty;
        public string? RiotId { get; set; } = string.Empty;

        public string Email { get; set; }
        public IEnumerable<GetTeamDTO> Teams { get; set; } = [];



        public GetPlayerDTO(Player player)
        {
            Id = player.Id;
            Username = player.Username;
            Firstname = player.Firstname;
            Lastname = player.Lastname;
            DiscordId = player.DiscordId;
            SteamId = player.SteamId;
            RiotId = player.RiotId;
            Email = player.Email;
            Teams = player.Teams.Select(t => new GetTeamDTO(t));
            Type = ParticipantType.Player;
        }       
    }
}
