using MercuriusAPI.DTOs.LAN.ParticipantDTOs;
using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.DTOs.LAN.MatchDTOs
{
    public class GetMatchDTO
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BracketType BracketType { get; set; }
        public GameFormat Format { get; set; }
        public ParticipantType ParticipantType { get; set; }

        public int RoundNumber { get; set; }
        public int MatchNumber { get; set; }
        public bool IsLowerBracketMatch { get; set; }

        public int GameId { get; set; }
        public int? Pariticipant1Id { get; set; }
        public int? Participant2Id { get; set; }
        public int? WinnerId { get; set; }
        public int? Participant1Score { get; set; }
        public int? Participant2Score { get; set; }

        public GetParticipantDTO? Participant1 { get; set; }
        public GetParticipantDTO? Participant2 { get; set; }
        public GetParticipantDTO? Winner { get; set; }

        public GetMatchDTO()
        {
            
        }

        public GetMatchDTO(Match match)
        {
            Id = match.Id;
            StartTime = match.StartTime;
            EndTime = match.EndTime;
            BracketType = match.BracketType;
            Format = match.Format;
            ParticipantType = match.ParticipantType;
            RoundNumber = match.RoundNumber;
            MatchNumber = match.MatchNumber;
            IsLowerBracketMatch = match.IsLowerBracketMatch;
            GameId = match.GameId;
            Pariticipant1Id = match.Pariticipant1Id;
            Participant2Id = match.Participant2Id;
            WinnerId = match.WinnerId;
            Participant1Score = match.Participant1Score;
            Participant2Score = match.Participant2Score;
            switch(ParticipantType)
            {
                case ParticipantType.Player:
                    Participant1 = match.Participant1 is not null ? new GetPlayerDTO((Player)match.Participant1): null;
                    Participant2 = match.Participant2 is not null ? new GetPlayerDTO((Player)match.Participant2): null;
                    Winner = match.Winner is not null ? new GetPlayerDTO((Player)match.Winner) : null;
                    break;
                case ParticipantType.Team:
                    Participant1 = match.Participant1 is not null ? new GetTeamDTO((Team)match.Participant1) : null;
                    Participant2 = match.Participant2 is not null ? new GetTeamDTO((Team)match.Participant2) : null;
                    Winner = match.Winner is not null ? new GetTeamDTO((Team)match.Winner): null;
                    break;
            }
        }
    }
}
