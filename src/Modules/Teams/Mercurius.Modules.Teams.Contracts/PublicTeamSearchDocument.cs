using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record PublicTeamSearchDocument(
    TeamId TeamId,
    string Name);
