using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record TournamentRegistrationCanceledIntegrationEvent(
    TournamentRegistrationId RegistrationId,
    GameId GameId,
    TeamId? TeamId);
