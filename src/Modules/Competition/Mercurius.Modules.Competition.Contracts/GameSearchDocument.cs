using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record GameSearchDocument(
    GameId GameId,
    string Name,
    string? ImageUrl);
