namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentRosterConfirmationChangedEvent(
    Guid TeamId,
    Guid RosterMemberId,
    Guid UserId,
    string Status);
