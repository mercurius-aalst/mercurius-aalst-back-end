using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentRegistrationCanceledIntegrationEvent(
    TournamentRegistrationId RegistrationId,
    TournamentId TournamentId,
    TeamId? TeamId);
