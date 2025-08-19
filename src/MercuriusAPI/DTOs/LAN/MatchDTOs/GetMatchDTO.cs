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
        public int? Participant1Id { get; set; }
        public int? Participant2Id { get; set; }
        public bool Participant1IsBYE { get; set; }
        public bool Participant2IsBYE { get; set; }
        public int? WinnerId { get; set; }
        public int? Participant1Score { get; set; }
        public int? Participant2Score { get; set; }
        public int? WinnerNextMatchId { get; set; }
        public int? LoserNextMatchId { get; set; }

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
            Participant1Id = match.Participant1Id;
            Participant2Id = match.Participant2Id;
            Participant1IsBYE = match.Participant1IsBYE;
            Participant2IsBYE = match.Participant2IsBYE;
            WinnerId = match.WinnerId;
            Participant1Score = match.Participant1Score;
            Participant2Score = match.Participant2Score;
            WinnerNextMatchId = match.WinnerNextMatchId;
            LoserNextMatchId = match.LoserNextMatchId;
        }
    }
}
