using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamRenamedIntegrationEvent(
    TeamId TeamId,
    long Version,
    string Name);
