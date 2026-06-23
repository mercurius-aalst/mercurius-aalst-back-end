using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamInviteChangedEvent(
    TeamId TeamId,
    TeamInviteId InviteId,
    UserId UserId,
    TeamInviteStatus Status);
