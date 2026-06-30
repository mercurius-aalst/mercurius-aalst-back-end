using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamMemberRemovedIntegrationEvent(
    TeamId TeamId,
    long Version,
    UserId UserId);
