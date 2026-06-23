using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamCaptainTransferredEvent(
    TeamId TeamId,
    UserId PreviousCaptainUserId,
    UserId NewCaptainUserId);
