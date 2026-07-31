using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record RosterMemberConfirmedIntegrationEvent(
    TournamentRegistrationId RegistrationId,
    UserId UserId,
    TeamId TeamId);
