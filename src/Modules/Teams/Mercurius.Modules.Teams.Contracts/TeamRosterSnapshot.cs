using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamRosterSnapshot(
    TeamId TeamId,
    string TeamName,
    UserId? CaptainUserId,
    string? LogoUrl,
    bool IsDeleted,
    IReadOnlyList<TeamMemberSnapshot> Members);
