namespace Mercurius.Modules.Teams.Contracts;

public sealed record TournamentRosterConfirmationChangedEvent(
    Guid TeamId,
    Guid RosterMemberId,
    Guid UserId,
    string Status);
