using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamMemberAddedIntegrationEvent(
    TeamId TeamId,
    long Version,
    UserId UserId);
