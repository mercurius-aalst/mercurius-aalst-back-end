using Mercurius.LAN.API.DTOs.RegistrationDTOs;

namespace Mercurius.LAN.API.Services.RegistrationServices;

public interface ITournamentRegistrationService
{
    Task<EligibilityResponseDTO> CheckIndividualEligibilityAsync(string auth0UserId, Guid gameId);
    Task<EligibilityResponseDTO> CheckTeamEligibilityAsync(string auth0UserId, Guid gameId, Guid teamId);
    Task<RosterCandidateEligibilityResponseDTO> CheckRosterEligibilityAsync(string auth0UserId, Guid gameId, Guid teamId, IReadOnlyList<Guid> userIds);
    Task<TournamentRegistrationDTO> RegisterIndividualAsync(string auth0UserId, Guid gameId);
    Task UnregisterIndividualAsync(string auth0UserId, Guid gameId);
    Task<TournamentRegistrationDTO> SubmitTeamRosterAsync(string auth0UserId, Guid gameId, SubmitTeamRosterDTO request);
    Task<TournamentRegistrationDTO> ConfirmRosterAsync(string auth0UserId, Guid rosterMemberId);
    Task UnregisterTeamAsync(string auth0UserId, Guid gameId, Guid teamId);
    Task<CurrentUserTournamentRegistrationStateDTO> GetCurrentUserStateAsync(string auth0UserId, Guid gameId);
    Task<IReadOnlyList<AdminTournamentRegistrationDTO>> GetAdminRegistrationsAsync(Guid gameId);
    Task RemoveIndividualAsAdminAsync(Guid gameId, Guid userId, string? reason, string? adminAuth0UserId);
    Task RemoveTeamAsAdminAsync(Guid gameId, Guid teamId, string? reason, string? adminAuth0UserId);
}
