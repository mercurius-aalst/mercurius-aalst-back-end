using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentSearchDocument(
    TournamentId TournamentId,
    string Name,
    string? ImageUrl);
