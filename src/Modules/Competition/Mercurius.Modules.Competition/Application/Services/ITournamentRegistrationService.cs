using Mercurius.Modules.Competition.Application.DTOs.Registrations;

namespace Mercurius.Modules.Competition.Application.Services;

internal interface ITournamentRegistrationService
{
    Task<EligibilityResponseDTO> CheckIndividualEligibilityAsync(string auth0UserId, Guid gameId, CancellationToken cancellationToken = default);
    Task<EligibilityResponseDTO> CheckTeamEligibilityAsync(string auth0UserId, Guid gameId, Guid teamId, CancellationToken cancellationToken = default);
    Task<RosterCandidateEligibilityResponseDTO> CheckRosterEligibilityAsync(string auth0UserId, Guid gameId, Guid teamId, IReadOnlyList<Guid> userIds, CancellationToken cancellationToken = default);
    Task<TournamentRegistrationDTO> RegisterIndividualAsync(string auth0UserId, Guid gameId, CancellationToken cancellationToken = default);
    Task UnregisterIndividualAsync(string auth0UserId, Guid gameId, CancellationToken cancellationToken = default);
    Task<TournamentRegistrationDTO> SubmitTeamRosterAsync(string auth0UserId, Guid gameId, SubmitTeamRosterDTO request, CancellationToken cancellationToken = default);
    Task<TournamentRegistrationDTO> ConfirmRosterAsync(string auth0UserId, Guid gameId, Guid rosterMemberId, CancellationToken cancellationToken = default);
    Task UnregisterTeamAsync(string auth0UserId, Guid gameId, Guid teamId, CancellationToken cancellationToken = default);
    Task<CurrentUserTournamentRegistrationStateDTO> GetCurrentUserStateAsync(string auth0UserId, Guid gameId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminTournamentRegistrationDTO>> GetAdminRegistrationsAsync(Guid gameId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task RemoveIndividualAsAdminAsync(Guid gameId, Guid userId, string? reason, string? adminAuth0UserId, CancellationToken cancellationToken = default);
    Task RemoveTeamAsAdminAsync(Guid gameId, Guid teamId, string? reason, string? adminAuth0UserId, CancellationToken cancellationToken = default);
}
