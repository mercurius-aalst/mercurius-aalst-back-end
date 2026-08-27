using Mercurius.Modules.Shared.Exceptions;

namespace Mercurius.Modules.Tournament.Domain;

internal sealed class Match
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
    public Guid? UserWinnerId { get; set; }
    public Guid? UserLoserId { get; set; }
    public Guid? TeamParticipant1Id { get; set; }
    public Guid? TeamParticipant2Id { get; set; }
    public Guid? TeamWinnerId { get; set; }
    public Guid? TeamLoserId { get; set; }
    public int? Participant1Score { get; set; }
    public int? Participant2Score { get; set; }
    public Guid? WinnerNextMatchId { get; set; }
    public Guid? LoserNextMatchId { get; set; }
    public bool Participant1IsBYE { get; set; }
    public bool Participant2IsBYE { get; set; }
    public TournamentAggregate Tournament { get; set; } = null!;
    public Match? WinnerNextMatch { get; set; }
    public Match? LoserNextMatch { get; set; }

    public void SetIndividualParticipants(Guid? participant1Id, Guid? participant2Id)
    {
        EnsureParticipationMode(ParticipationMode.Individual);
        ClearTeamAssignments();
        UserParticipant1Id = participant1Id;
        UserParticipant2Id = participant2Id;
    }

    public void SetTeamParticipants(Guid? participant1Id, Guid? participant2Id)
    {
        EnsureParticipationMode(ParticipationMode.Team);
        ClearUserAssignments();
        TeamParticipant1Id = participant1Id;
        TeamParticipant2Id = participant2Id;
    }

    public void TryAssignByeWin()
    {
        if (!(Participant1IsBYE || Participant2IsBYE))
            return;

        if (!HasParticipant1() && HasParticipant2())
            AssignWinner(GetParticipant2Id());
        else if (!HasParticipant2() && HasParticipant1())
            AssignWinner(GetParticipant1Id());
    }

    public void SetParticipantBYEs(bool participant1BYE, bool participant2BYE)
    {
        if (RoundNumber != 1 && participant1BYE && participant2BYE)
            return;

        Participant1IsBYE |= participant1BYE;
        Participant2IsBYE |= participant2BYE;
    }

    public void Start() => StartTime = DateTime.UtcNow;

    public void Finish()
    {
        EndTime = DateTime.UtcNow;
        UpdateParticipantsNextMatch();
    }

    public void SetEstimatedWindow(DateTime estimatedStartTime, DateTime estimatedEndTime)
    {
        if (estimatedEndTime <= estimatedStartTime)
            throw new ValidationException("Estimated match end time must be greater than estimated start time.");

        EstimatedStartTime = estimatedStartTime;
        EstimatedEndTime = estimatedEndTime;
    }

    public void SetScoresAndWinner(int participant1Score, int participant2Score)
    {
        if (participant1Score < 0 || participant2Score < 0)
            throw new ValidationException("Scores cannot be negative");

        var winsNeeded = Format switch
        {
            GameFormat.BestOf1 => 1,
            GameFormat.BestOf3 => 2,
            GameFormat.BestOf5 => 3,
            _ => 1
        };

        if (participant1Score > winsNeeded || participant2Score > winsNeeded)
            throw new ValidationException("Scores cannot exceed the required number of wins for the match format.");
        if (participant1Score == participant2Score && participant1Score + participant2Score != 0 && winsNeeded == 1)
            throw new ValidationException("Scores cannot be equal in Bo1 format");

        Participant1Score = participant1Score;
        Participant2Score = participant2Score;

        if (participant1Score == winsNeeded && participant1Score > participant2Score)
        {
            SetWinnerAndLoser(GetParticipant1Id(), GetParticipant2Id());
            Finish();
        }
        else if (participant2Score == winsNeeded && participant2Score > participant1Score)
        {
            SetWinnerAndLoser(GetParticipant2Id(), GetParticipant1Id());
            Finish();
        }
    }

    public void UpdateParticipantsNextMatch()
    {
        if (!HasWinner())
            return;

        if (WinnerNextMatch is not null)
        {
            if (WinnerNextMatch.IsLowerBracketMatch)
            {
                if (!WinnerNextMatch.HasParticipant2() || WinnerNextMatch.GetParticipant2Id() == GetWinnerId())
                    AssignWinnerToParticipant2(WinnerNextMatch);
                else
                    AssignWinnerToParticipant1(WinnerNextMatch);

                if (WinnerNextMatch.Participant1IsBYE || WinnerNextMatch.Participant2IsBYE)
                    WinnerNextMatch.TryAssignByeWin();
            }
            else if (MatchNumber % 2 != 0 && !IsLowerBracketMatch)
            {
                AssignWinnerToParticipant1(WinnerNextMatch);
            }
            else
            {
                AssignWinnerToParticipant2(WinnerNextMatch);
            }
        }

        if (LoserNextMatch is null)
            return;

        if (RoundNumber == 1 && MatchNumber % 2 == 0)
            AssignLoserToParticipant2(LoserNextMatch);
        else
            AssignLoserToParticipant1(LoserNextMatch);

        if (LoserNextMatch.Participant1IsBYE || LoserNextMatch.Participant2IsBYE)
            LoserNextMatch.TryAssignByeWin();
    }

    public void SetIndividualParticipant1(Guid? participantId)
    {
        EnsureParticipationMode(ParticipationMode.Individual);
        ClearTeamAssignments();
        UserParticipant1Id = participantId;
    }

    public void SetIndividualParticipant2(Guid? participantId)
    {
        EnsureParticipationMode(ParticipationMode.Individual);
        ClearTeamAssignments();
        UserParticipant2Id = participantId;
    }

    public void SetTeamParticipant1(Guid? participantId)
    {
        EnsureParticipationMode(ParticipationMode.Team);
        ClearUserAssignments();
        TeamParticipant1Id = participantId;
    }

    public void SetTeamParticipant2(Guid? participantId)
    {
        EnsureParticipationMode(ParticipationMode.Team);
        ClearUserAssignments();
        TeamParticipant2Id = participantId;
    }

    public bool HasParticipant1() => GetParticipant1Id().HasValue;

    public bool HasParticipant2() => GetParticipant2Id().HasValue;

    public bool HasWinner() => GetWinnerId().HasValue;

    public Guid? GetParticipant1Id()
    {
        return ParticipationMode == ParticipationMode.Individual
            ? UserParticipant1Id
            : TeamParticipant1Id;
    }

    public Guid? GetParticipant2Id()
    {
        return ParticipationMode == ParticipationMode.Individual
            ? UserParticipant2Id
            : TeamParticipant2Id;
    }

    public Guid? GetWinnerId()
    {
        return ParticipationMode == ParticipationMode.Individual
            ? UserWinnerId
            : TeamWinnerId;
    }

    private void AssignWinner(Guid? winnerId)
    {
        if (ParticipationMode == ParticipationMode.Individual)
        {
            ClearTeamAssignments();
            UserWinnerId = winnerId;
            UserLoserId = null;
        }
        else
        {
            ClearUserAssignments();
            TeamWinnerId = winnerId;
            TeamLoserId = null;
        }

        UpdateParticipantsNextMatch();
    }

    private void SetWinnerAndLoser(Guid? winnerId, Guid? loserId)
    {
        if (ParticipationMode == ParticipationMode.Individual)
        {
            ClearTeamAssignments();
            UserWinnerId = winnerId;
            UserLoserId = loserId;
        }
        else
        {
            ClearUserAssignments();
            TeamWinnerId = winnerId;
            TeamLoserId = loserId;
        }
    }

    private void AssignWinnerToParticipant1(Match targetMatch) =>
        targetMatch.SetParticipant1(GetWinnerId());

    private void AssignWinnerToParticipant2(Match targetMatch) =>
        targetMatch.SetParticipant2(GetWinnerId());

    private void AssignLoserToParticipant1(Match targetMatch) =>
        targetMatch.SetParticipant1(GetLoserId());

    private void AssignLoserToParticipant2(Match targetMatch) =>
        targetMatch.SetParticipant2(GetLoserId());

    private void SetParticipant1(Guid? participantId)
    {
        if (ParticipationMode == ParticipationMode.Individual)
            SetIndividualParticipant1(participantId);
        else
            SetTeamParticipant1(participantId);
    }

    private void SetParticipant2(Guid? participantId)
    {
        if (ParticipationMode == ParticipationMode.Individual)
            SetIndividualParticipant2(participantId);
        else
            SetTeamParticipant2(participantId);
    }

    private Guid? GetLoserId()
    {
        return ParticipationMode == ParticipationMode.Individual
            ? UserLoserId
            : TeamLoserId;
    }

    private void EnsureParticipationMode(ParticipationMode expectedMode)
    {
        if (ParticipationMode != expectedMode)
            throw new ValidationException($"Match only accepts {expectedMode.ToString().ToLowerInvariant()} participants.");
    }

    private void ClearUserAssignments()
    {
        UserParticipant1Id = null;
        UserParticipant2Id = null;
        UserWinnerId = null;
        UserLoserId = null;
    }

    private void ClearTeamAssignments()
    {
        TeamParticipant1Id = null;
        TeamParticipant2Id = null;
        TeamWinnerId = null;
        TeamLoserId = null;
    }
}
