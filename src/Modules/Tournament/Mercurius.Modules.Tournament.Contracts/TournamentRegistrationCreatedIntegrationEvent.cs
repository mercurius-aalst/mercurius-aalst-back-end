using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentRegistrationCreatedIntegrationEvent(
    TournamentRegistrationId RegistrationId,
    TournamentId TournamentId,
    UserId RegisteredByUserId,
    TeamId? TeamId);
