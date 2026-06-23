using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamMembershipChangedEvent(
    TeamId TeamId,
    UserId UserId,
    TeamMembershipChangeKind ChangeKind);
