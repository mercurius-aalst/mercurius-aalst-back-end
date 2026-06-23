using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record TournamentRosterMemberSnapshot(
    TournamentRosterMemberId Id,
    UserId UserId,
    TeamId? TeamId,
    bool IsCaptain,
    RosterMemberConfirmationStatus ConfirmationStatus);
