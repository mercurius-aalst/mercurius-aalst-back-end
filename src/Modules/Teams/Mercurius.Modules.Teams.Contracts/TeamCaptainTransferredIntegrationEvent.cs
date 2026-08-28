using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamCaptainTransferredIntegrationEvent(
    TeamId TeamId,
    long Version,
    UserId NewCaptainUserId);
