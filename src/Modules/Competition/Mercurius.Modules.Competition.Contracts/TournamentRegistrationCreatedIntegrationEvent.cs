using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record TournamentRegistrationCreatedIntegrationEvent(
    TournamentRegistrationId RegistrationId,
    GameId GameId,
    UserId RegisteredByUserId,
    TeamId? TeamId);
