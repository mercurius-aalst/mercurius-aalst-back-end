namespace Mercurius.Modules.Competition.Contracts;

public sealed record TournamentRosterConfirmationChangedEvent(
    Guid TeamId,
    Guid RosterMemberId,
    Guid UserId,
    string Status);
