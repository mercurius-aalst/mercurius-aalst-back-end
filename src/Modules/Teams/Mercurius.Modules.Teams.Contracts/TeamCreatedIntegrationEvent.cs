using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamCreatedIntegrationEvent(
    TeamId TeamId,
    long Version,
    string Name,
    UserId CaptainUserId);
