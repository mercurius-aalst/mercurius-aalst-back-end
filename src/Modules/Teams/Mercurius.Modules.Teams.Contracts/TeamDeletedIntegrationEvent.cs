using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamDeletedIntegrationEvent(
    TeamId TeamId,
    long Version);
