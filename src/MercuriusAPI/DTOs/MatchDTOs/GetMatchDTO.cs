using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.MatchDTOs;

public class GetMatchDTO
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BracketType BracketType { get; set; }
    public GameFormat Format { get; set; }
    public ParticipationMode ParticipationMode { get; set; }

    public int RoundNumber { get; set; }
    public int MatchNumber { get; set; }
    public bool IsLowerBracketMatch { get; set; }

    public int GameId { get; set; }
    public Guid? UserParticipant1Id { get; set; }
    public Guid? UserParticipant2Id { get; set; }
    public int? TeamParticipant1Id { get; set; }
    public int? TeamParticipant2Id { get; set; }
    public bool Participant1IsBYE { get; set; }
    public bool Participant2IsBYE { get; set; }
    public Guid? UserWinnerId { get; set; }
    public Guid? UserLoserId { get; set; }
    public int? TeamWinnerId { get; set; }
    public int? TeamLoserId { get; set; }
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
        ParticipationMode = match.ParticipationMode;
        RoundNumber = match.RoundNumber;
        MatchNumber = match.MatchNumber;
        IsLowerBracketMatch = match.IsLowerBracketMatch;
        GameId = match.GameId;
        UserParticipant1Id = match.UserParticipant1Id;
        UserParticipant2Id = match.UserParticipant2Id;
        TeamParticipant1Id = match.TeamParticipant1Id;
        TeamParticipant2Id = match.TeamParticipant2Id;
        Participant1IsBYE = match.Participant1IsBYE;
        Participant2IsBYE = match.Participant2IsBYE;
        UserWinnerId = match.UserWinnerId;
        UserLoserId = match.UserLoserId;
        TeamWinnerId = match.TeamWinnerId;
        TeamLoserId = match.TeamLoserId;
        Participant1Score = match.Participant1Score;
        Participant2Score = match.Participant2Score;
        WinnerNextMatchId = match.WinnerNextMatchId;
        LoserNextMatchId = match.LoserNextMatchId;
    }
}

