using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record RosterMemberConfirmedIntegrationEvent(
    TournamentRegistrationId RegistrationId,
    UserId UserId,
    TeamId TeamId);
