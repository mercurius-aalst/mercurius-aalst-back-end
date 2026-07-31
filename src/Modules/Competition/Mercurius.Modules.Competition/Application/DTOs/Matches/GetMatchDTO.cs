using Mercurius.Modules.Competition.Contracts;

namespace Mercurius.Modules.Competition.Application.DTOs.Matches;

public class GetMatchDTO
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime? EstimatedStartTime { get; set; }
    public DateTime? EstimatedEndTime { get; set; }
    public BracketType BracketType { get; set; }
    public GameFormat Format { get; set; }
    public ParticipationMode ParticipationMode { get; set; }

    public int RoundNumber { get; set; }
    public int MatchNumber { get; set; }
    public bool IsLowerBracketMatch { get; set; }

    public Guid GameId { get; set; }
    public Guid? UserParticipant1Id { get; set; }
    public Guid? UserParticipant2Id { get; set; }
    public Guid? TeamParticipant1Id { get; set; }
    public Guid? TeamParticipant2Id { get; set; }
    public bool Participant1IsBYE { get; set; }
    public bool Participant2IsBYE { get; set; }
    public Guid? UserWinnerId { get; set; }
    public Guid? UserLoserId { get; set; }
    public Guid? TeamWinnerId { get; set; }
    public Guid? TeamLoserId { get; set; }
    public int? Participant1Score { get; set; }
    public int? Participant2Score { get; set; }
    public Guid? WinnerNextMatchId { get; set; }
    public Guid? LoserNextMatchId { get; set; }

    public GetMatchDTO()
    {

    }

}

