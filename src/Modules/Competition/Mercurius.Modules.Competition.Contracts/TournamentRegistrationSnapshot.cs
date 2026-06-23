using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record TournamentRegistrationSnapshot(
    TournamentRegistrationId Id,
    GameId GameId,
    TournamentRegistrationKind Kind,
    TournamentRegistrationStatus Status,
    UserId? UserId,
    TeamId? TeamId,
    IReadOnlyList<TournamentRosterMemberSnapshot> RosterMembers,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
