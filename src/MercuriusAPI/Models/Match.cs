using Mercurius.LAN.API.Exceptions;

namespace Mercurius.LAN.API.Models;

public class Match
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

    public Game Game { get; set; }
    public User? UserParticipant1 { get; set; }
    public User? UserParticipant2 { get; set; }
    public User? UserWinner { get; set; }
    public User? UserLoser { get; set; }
    public Team? TeamParticipant1 { get; set; }
    public Team? TeamParticipant2 { get; set; }
    public Team? TeamWinner { get; set; }
    public Team? TeamLoser { get; set; }

    public Match? WinnerNextMatch { get; set; }
    public Match? LoserNextMatch { get; set; }

    public void SetParticipants(User? participant1, User? participant2)
    {
        EnsureParticipationMode(ParticipationMode.Individual);
        ClearTeamAssignments();
        UserParticipant1 = participant1;
        UserParticipant2 = participant2;
        UserParticipant1Id = participant1?.Id;
        UserParticipant2Id = participant2?.Id;
    }

    public void SetParticipants(Team? participant1, Team? participant2)
    {
        EnsureParticipationMode(ParticipationMode.Team);
        ClearUserAssignments();
        TeamParticipant1 = participant1;
        TeamParticipant2 = participant2;
        TeamParticipant1Id = participant1?.Id;
        TeamParticipant2Id = participant2?.Id;
    }

    public void TryAssignByeWin()
    {
        if (!(Participant1IsBYE || Participant2IsBYE))
            return;

        switch (ParticipationMode)
        {
            case ParticipationMode.Individual:
                if (UserParticipant1 == null && UserParticipant2 != null)
                    AssignWinner(UserParticipant2);
                else if (UserParticipant2 == null && UserParticipant1 != null)
                    AssignWinner(UserParticipant1);
                break;
            case ParticipationMode.Team:
                if (TeamParticipant1 == null && TeamParticipant2 != null)
                    AssignWinner(TeamParticipant2);
                else if (TeamParticipant2 == null && TeamParticipant1 != null)
                    AssignWinner(TeamParticipant1);
                break;
        }
    }

    public void SetParticipantBYEs(bool participant1BYE, bool participant2BYE)
    {
        if (RoundNumber != 1 && participant1BYE && participant2BYE)
            return;

        Participant1IsBYE = Participant1IsBYE || participant1BYE;
        Participant2IsBYE = Participant2IsBYE || participant2BYE;
    }

    private void AssignWinner(User? winner)
    {
        EnsureParticipationMode(ParticipationMode.Individual);
        ClearTeamAssignments();
        UserWinner = winner;
        UserWinnerId = winner?.Id;
        UserLoser = null;
        UserLoserId = null;
        UpdateParticipantsNextMatch();
    }

    private void AssignWinner(Team? winner)
    {
        EnsureParticipationMode(ParticipationMode.Team);
        ClearUserAssignments();
        TeamWinner = winner;
        TeamWinnerId = winner?.Id;
        TeamLoser = null;
        TeamLoserId = null;
        UpdateParticipantsNextMatch();
    }

    public void Start()
    {
        StartTime = DateTime.UtcNow;
    }

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
        int winsNeeded = Format switch
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
            switch (ParticipationMode)
            {
                case ParticipationMode.Individual:
                    SetWinnerAndLoser(UserParticipant1, UserParticipant2);
                    break;
                case ParticipationMode.Team:
                    SetWinnerAndLoser(TeamParticipant1, TeamParticipant2);
                    break;
            }
            Finish();
        }
        else if (participant2Score == winsNeeded && participant2Score > participant1Score)
        {
            switch (ParticipationMode)
            {
                case ParticipationMode.Individual:
                    SetWinnerAndLoser(UserParticipant2, UserParticipant1);
                    break;
                case ParticipationMode.Team:
                    SetWinnerAndLoser(TeamParticipant2, TeamParticipant1);
                    break;
            }
            Finish();
        }
    }

    public void UpdateParticipantsNextMatch()
    {
        if (!HasWinner())
            return;

        if (WinnerNextMatch != null)
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
            else
            {
                if (MatchNumber % 2 != 0 && !IsLowerBracketMatch)
                    AssignWinnerToParticipant1(WinnerNextMatch);
                else
                    AssignWinnerToParticipant2(WinnerNextMatch);
            }
        }

        if (LoserNextMatch != null)
        {
            if (RoundNumber == 1)
            {
                if (MatchNumber % 2 != 0)
                    AssignLoserToParticipant1(LoserNextMatch);
                else
                    AssignLoserToParticipant2(LoserNextMatch);
            }
            else
            {
                AssignLoserToParticipant1(LoserNextMatch);
            }

            if (LoserNextMatch.Participant1IsBYE || LoserNextMatch.Participant2IsBYE)
                LoserNextMatch.TryAssignByeWin();
        }
    }

    public void SetParticipant1(User? participant)
    {
        EnsureParticipationMode(ParticipationMode.Individual);
        ClearTeamAssignments();
        UserParticipant1 = participant;
        UserParticipant1Id = participant?.Id;
    }

    public void SetParticipant2(User? participant)
    {
        EnsureParticipationMode(ParticipationMode.Individual);
        ClearTeamAssignments();
        UserParticipant2 = participant;
        UserParticipant2Id = participant?.Id;
    }

    public void SetParticipant1(Team? participant)
    {
        EnsureParticipationMode(ParticipationMode.Team);
        ClearUserAssignments();
        TeamParticipant1 = participant;
        TeamParticipant1Id = participant?.Id;
    }

    public void SetParticipant2(Team? participant)
    {
        EnsureParticipationMode(ParticipationMode.Team);
        ClearUserAssignments();
        TeamParticipant2 = participant;
        TeamParticipant2Id = participant?.Id;
    }

    private void SetWinnerAndLoser(User? winner, User? loser)
    {
        EnsureParticipationMode(ParticipationMode.Individual);
        ClearTeamAssignments();
        UserWinner = winner;
        UserWinnerId = winner?.Id;
        UserLoser = loser;
        UserLoserId = loser?.Id;
    }

    private void SetWinnerAndLoser(Team? winner, Team? loser)
    {
        EnsureParticipationMode(ParticipationMode.Team);
        ClearUserAssignments();
        TeamWinner = winner;
        TeamWinnerId = winner?.Id;
        TeamLoser = loser;
        TeamLoserId = loser?.Id;
    }

    public bool HasParticipant1()
    {
        return ParticipationMode switch
        {
            ParticipationMode.Individual => UserParticipant1 != null,
            ParticipationMode.Team => TeamParticipant1 != null,
            _ => false
        };
    }

    public bool HasParticipant2()
    {
        return ParticipationMode switch
        {
            ParticipationMode.Individual => UserParticipant2 != null,
            ParticipationMode.Team => TeamParticipant2 != null,
            _ => false
        };
    }

    public bool HasWinner()
    {
        return ParticipationMode switch
        {
            ParticipationMode.Individual => UserWinner != null,
            ParticipationMode.Team => TeamWinner != null,
            _ => false
        };
    }

    public Guid? GetParticipant1Id()
    {
        return ParticipationMode switch
        {
            ParticipationMode.Individual => UserParticipant1Id,
            ParticipationMode.Team => TeamParticipant1Id,
            _ => null
        };
    }

    public Guid? GetParticipant2Id()
    {
        return ParticipationMode switch
        {
            ParticipationMode.Individual => UserParticipant2Id,
            ParticipationMode.Team => TeamParticipant2Id,
            _ => null
        };
    }

    public Guid? GetWinnerId()
    {
        return ParticipationMode switch
        {
            ParticipationMode.Individual => UserWinnerId,
            ParticipationMode.Team => TeamWinnerId,
            _ => null
        };
    }

    private void AssignWinnerToParticipant1(Match targetMatch)
    {
        switch (ParticipationMode)
        {
            case ParticipationMode.Individual:
                targetMatch.SetParticipant1(UserWinner);
                break;
            case ParticipationMode.Team:
                targetMatch.SetParticipant1(TeamWinner);
                break;
        }
    }

    private void AssignWinnerToParticipant2(Match targetMatch)
    {
        switch (ParticipationMode)
        {
            case ParticipationMode.Individual:
                targetMatch.SetParticipant2(UserWinner);
                break;
            case ParticipationMode.Team:
                targetMatch.SetParticipant2(TeamWinner);
                break;
        }
    }

    private void AssignLoserToParticipant1(Match targetMatch)
    {
        switch (ParticipationMode)
        {
            case ParticipationMode.Individual:
                targetMatch.SetParticipant1(UserLoser);
                break;
            case ParticipationMode.Team:
                targetMatch.SetParticipant1(TeamLoser);
                break;
        }
    }

    private void AssignLoserToParticipant2(Match targetMatch)
    {
        switch (ParticipationMode)
        {
            case ParticipationMode.Individual:
                targetMatch.SetParticipant2(UserLoser);
                break;
            case ParticipationMode.Team:
                targetMatch.SetParticipant2(TeamLoser);
                break;
        }
    }

    private void EnsureParticipationMode(ParticipationMode expectedMode)
    {
        if (ParticipationMode != expectedMode)
            throw new ValidationException($"Match only accepts {expectedMode.ToString().ToLowerInvariant()} participants.");
    }

    private void ClearUserAssignments()
    {
        UserParticipant1 = null;
        UserParticipant2 = null;
        UserWinner = null;
        UserLoser = null;
        UserParticipant1Id = null;
        UserParticipant2Id = null;
        UserWinnerId = null;
        UserLoserId = null;
    }

    private void ClearTeamAssignments()
    {
        TeamParticipant1 = null;
        TeamParticipant2 = null;
        TeamWinner = null;
        TeamLoser = null;
        TeamParticipant1Id = null;
        TeamParticipant2Id = null;
        TeamWinnerId = null;
        TeamLoserId = null;
    }
}

