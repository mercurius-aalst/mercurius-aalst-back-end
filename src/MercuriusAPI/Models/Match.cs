using Mercurius.LAN.API.Exceptions;

namespace Mercurius.LAN.API.Models;

public class Match
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
    public int? Participant1Id { get; set; }
    public int? Participant2Id { get; set; }
    public int? WinnerId { get; set; }
    public int? LoserId { get; set; }

    public int? Participant1Score { get; set; }
    public int? Participant2Score { get; set; }

    public int? WinnerNextMatchId { get; set; }
    public int? LoserNextMatchId { get; set; }

    public bool Participant1IsBYE { get; set; }
    public bool Participant2IsBYE { get; set; }

    public Game Game { get; set; }
    public Participant? Participant1 { get; set; }
    public Participant? Participant2 { get; set; }
    public Participant? Winner { get; set; }
    public Participant? Loser { get; set; }

    public Match? WinnerNextMatch { get; set; }
    public Match? LoserNextMatch { get; set; }

    public void SetParticipants(User? participant1, User? participant2)
    {
        ParticipationMode = ParticipationMode.Individual;
        SetParticipantsCore(participant1, participant2);
    }

    public void SetParticipants(Team? participant1, Team? participant2)
    {
        ParticipationMode = ParticipationMode.Team;
        SetParticipantsCore(participant1, participant2);
    }

    public void SetParticipants(Participant? participant1, Participant? participant2)
    {
        EnsureParticipantMatchesMode(participant1, nameof(participant1));
        EnsureParticipantMatchesMode(participant2, nameof(participant2));
        SetParticipantsCore(participant1, participant2);
    }

    public void TryAssignByeWin()
    {
        NormalizeParticipationModeFromAssignedParticipants();

        if (Participant1IsBYE || Participant2IsBYE)
        {
            if ((Participant1 == null && Participant2 != null))
            {
                AssignWinner(Participant2, isParticipant1Bye: true);
            }
            else if ((Participant2 == null && Participant1 != null))
            {
                AssignWinner(Participant1, isParticipant2Bye: true);
            }
        }
    }

    public void SetParticipantBYEs(bool participant1BYE, bool participant2BYE)
    {
        NormalizeParticipationModeFromAssignedParticipants();

        if (RoundNumber != 1 && participant1BYE && participant2BYE)
            return;

        Participant1IsBYE = Participant1IsBYE || participant1BYE;
        Participant2IsBYE = Participant2IsBYE || participant2BYE;
    }

    private void AssignWinner(Participant? winner, bool isParticipant1Bye = false, bool isParticipant2Bye = false)
    {
        Winner = winner;
        Loser = null;
        UpdateParticipantsNextMatch();
    }

    public void Start()
    {
        StartTime = DateTime.UtcNow;
    }

    public void Finish()
    {
        NormalizeParticipationModeFromAssignedParticipants();
        EndTime = DateTime.UtcNow;
        UpdateParticipantsNextMatch();
    }

    public void SetScoresAndWinner(int participant1Score, int participant2Score)
    {
        NormalizeParticipationModeFromAssignedParticipants();

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
            SetWinnerAndLoser(Participant1, Participant2);
            Finish();
        }
        else if (participant2Score == winsNeeded && participant2Score > participant1Score)
        {
            SetWinnerAndLoser(Participant2, Participant1);
            Finish();
        }
    }

    public void UpdateParticipantsNextMatch()
    {
        NormalizeParticipationModeFromAssignedParticipants();

        if (Winner == null)
            return;

        // Assign Winner to the next match
        if (WinnerNextMatch != null)
        {
            if (WinnerNextMatch.IsLowerBracketMatch)
            {
                if (WinnerNextMatch.Participant2 == null || WinnerNextMatch.Participant2?.Id == Winner.Id)
                    WinnerNextMatch.SetParticipant2(Winner);
                else
                    WinnerNextMatch.SetParticipant1(Winner);

                if (WinnerNextMatch.Participant1IsBYE || WinnerNextMatch.Participant2IsBYE)
                    WinnerNextMatch.TryAssignByeWin();
            }
            else
            {
                if (MatchNumber % 2 != 0 && !IsLowerBracketMatch)
                    WinnerNextMatch.SetParticipant1(Winner);
                else
                    WinnerNextMatch.SetParticipant2(Winner);
            }
        }

        // Assign Loser to the next match
        if (LoserNextMatch != null)
        {
            if (RoundNumber == 1)
            {
                if (MatchNumber % 2 != 0)
                    LoserNextMatch.SetParticipant1(Loser);
                else
                    LoserNextMatch.SetParticipant2(Loser);
            }
            else
            {
                LoserNextMatch.SetParticipant1(Loser);
            }
            if (LoserNextMatch.Participant1IsBYE || LoserNextMatch.Participant2IsBYE)
                LoserNextMatch.TryAssignByeWin();
        }
    }

    public void SetParticipant1(Participant? participant)
    {
        AdoptParticipationModeIfUnset(participant);
        EnsureParticipantMatchesMode(participant, nameof(Participant1));
        Participant1 = participant;
    }

    public void SetParticipant2(Participant? participant)
    {
        AdoptParticipationModeIfUnset(participant);
        EnsureParticipantMatchesMode(participant, nameof(Participant2));
        Participant2 = participant;
    }

    private void SetParticipantsCore(Participant? participant1, Participant? participant2)
    {
        AdoptParticipationModeIfUnset(participant1 ?? participant2);
        Participant1 = participant1;
        Participant2 = participant2;
    }

    private void SetWinnerAndLoser(Participant? winner, Participant? loser)
    {
        AdoptParticipationModeIfUnset(winner ?? loser);
        EnsureParticipantMatchesMode(winner, nameof(Winner));
        EnsureParticipantMatchesMode(loser, nameof(Loser));
        Winner = winner;
        Loser = loser;
    }

    private void AdoptParticipationModeIfUnset(Participant? participant)
    {
        if (participant is null || HasResolvedMode())
            return;

        ParticipationMode = participant switch
        {
            User => ParticipationMode.Individual,
            Team => ParticipationMode.Team,
            _ => ParticipationMode
        };
    }

    private bool HasResolvedMode()
    {
        return Participant1 != null
            || Participant2 != null
            || Winner != null
            || Loser != null
            || Participant1Id.HasValue
            || Participant2Id.HasValue
            || WinnerId.HasValue
            || LoserId.HasValue;
    }

    private void NormalizeParticipationModeFromAssignedParticipants()
    {
        var participant = Participant1 ?? Participant2 ?? Winner ?? Loser;
        if (participant is null)
            return;

        ParticipationMode = participant switch
        {
            User => ParticipationMode.Individual,
            Team => ParticipationMode.Team,
            _ => ParticipationMode
        };
    }

    private void EnsureParticipantMatchesMode(Participant? participant, string parameterName)
    {
        if (participant is null)
            return;

        bool isCompatible = ParticipationMode switch
        {
            ParticipationMode.Individual => participant is User,
            ParticipationMode.Team => participant is Team,
            _ => false
        };

        if (!isCompatible)
            throw new ValidationException($"{parameterName} is incompatible with {ParticipationMode} match mode.");
    }
}

