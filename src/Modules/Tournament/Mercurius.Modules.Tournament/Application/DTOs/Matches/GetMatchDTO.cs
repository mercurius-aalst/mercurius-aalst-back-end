using Mercurius.Modules.Tournament.Contracts;

namespace Mercurius.Modules.Tournament.Application.DTOs.Matches;

internal class GetMatchDTO
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

    public Guid TournamentId { get; set; }
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
    public MatchLifecycleState LifecycleState { get; set; }
    public bool Participant1Ended { get; set; }
    public bool Participant2Ended { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public int? Participant1ReportedScore1 { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public int? Participant1ReportedScore2 { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public int? Participant2ReportedScore1 { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public int? Participant2ReportedScore2 { get; set; }
    public DateTime? ScoreConfirmationDeadlineUtc { get; set; }
    public DateTime? CorrectionDeadlineUtc { get; set; }
    public int Participant1CorrectionCount { get; set; }
    public int Participant2CorrectionCount { get; set; }
    public int? ForfeitedParticipantNumber { get; set; }
    public MatchResultKind? ResultKind { get; set; }
    public int ResultVersion { get; set; }

    public GetMatchDTO()
    {

    }

}

