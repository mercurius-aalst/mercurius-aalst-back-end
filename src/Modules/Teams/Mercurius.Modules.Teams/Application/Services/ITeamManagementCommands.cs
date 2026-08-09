using Mercurius.Modules.Teams.DTOs;

namespace Mercurius.Modules.Teams.Services;

internal interface ITeamManagementCommands
{
    Task<TeamManagementSummaryDTO> CreateCurrentUserTeamAsync(string auth0UserId, CreateTeamDTO teamDTO, CancellationToken cancellationToken = default);
    Task DeleteTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamManagementSummaryDTO> LeaveTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamManagementSummaryDTO> RemoveMemberAsync(string auth0UserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default);
    Task<TeamManagementSummaryDTO> TransferCaptainAsync(string auth0UserId, Guid teamId, Guid newCaptainUserId, CancellationToken cancellationToken = default);
}
