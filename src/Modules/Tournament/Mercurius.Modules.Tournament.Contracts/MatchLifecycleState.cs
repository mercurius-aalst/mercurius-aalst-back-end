namespace Mercurius.Modules.Tournament.Contracts;

public enum MatchLifecycleState
{
    AwaitingEndedConfirmation = 0,
    AwaitingScore = 1,
    ScoreConfirmation = 2,
    Disputed = 3,
    AdminResolutionRequired = 4,
    Completed = 5,
    Forfeited = 6,
    Reversed = 7
}
