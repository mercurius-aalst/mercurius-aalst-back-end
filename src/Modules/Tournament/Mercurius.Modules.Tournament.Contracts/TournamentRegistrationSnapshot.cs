using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentRegistrationSnapshot(
    TournamentRegistrationId Id,
    TournamentId TournamentId,
    TournamentRegistrationKind Kind,
    TournamentRegistrationStatus Status,
    UserId? UserId,
    TeamId? TeamId,
    IReadOnlyList<TournamentRosterMemberSnapshot> RosterMembers,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
