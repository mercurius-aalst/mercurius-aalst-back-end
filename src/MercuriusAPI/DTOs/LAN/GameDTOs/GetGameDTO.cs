using MercuriusAPI.DTOs.LAN.MatchDTOs;
using MercuriusAPI.DTOs.LAN.ParticipantDTOs;
using MercuriusAPI.DTOs.LAN.PlacementDTOs;
using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.DTOs.LAN.GameDTOs
{
    public class GetGameDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PictureUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public GameStatus Status { get; set; }
        public BracketType BracketType { get; set; }
        public GameFormat Format { get; set; }
        public GameFormat FinalsFormat { get; set; }
        public ParticipantType ParticipantType { get; set; }

        public IEnumerable<GetPlacementDTO> Placements { get; set; } = [];

        public IEnumerable<GetMatchDTO> Matches { get; set; } = [];
        public IEnumerable<GetParticipantDTO> Participants { get; set; } = [];

        public GetGameDTO(Game game)
        {
            Id = game.Id;
            Name = game.Name;
            PictureUrl = game.PictureUrl;
            StartTime = game.StartTime;
            EndTime = game.EndTime;
            Status = game.Status;
            BracketType = game.BracketType;
            Format = game.Format;
            FinalsFormat = game.FinalsFormat;
            ParticipantType = game.ParticipantType;
            Placements = game.Placements.Select(p => new GetPlacementDTO(p, game.ParticipantType));
            Matches = game.Matches.Select(m => new GetMatchDTO(m));
            switch(ParticipantType)
            {
                case ParticipantType.Player:
                    Participants = game.Participants.Select(p => new GetPlayerDTO((Player)p)).ToList();
                    break;
                case ParticipantType.Team:
                    Participants = game.Participants.Select(t => new GetTeamDTO((Team)t)).ToList();
                    break;
            }
        }

        public GetGameDTO()
        {
        }
    }
}
