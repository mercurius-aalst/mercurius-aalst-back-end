using Mercurius.Modules.Tournament.Application.DTOs.Leaderboards;

namespace Mercurius.Modules.Tournament.Application.Services;

internal interface ILeaderboardService
{
    Task<LeaderboardResponseDTO> GetPublicLeaderboardAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    Task<AdminLeaderboardResponseDTO> GetAdminLeaderboardAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    Task<AdminLeaderboardParticipantDTO> RecordAttemptAsync(Guid tournamentId, RecordLeaderboardAttemptDTO request, CancellationToken cancellationToken = default);
    Task<LeaderboardAttemptDTO> UpdateAttemptAsync(Guid tournamentId, Guid attemptId, UpdateLeaderboardAttemptDTO request, CancellationToken cancellationToken = default);
    Task RemoveAttemptAsync(Guid tournamentId, Guid attemptId, Guid rowVersion, CancellationToken cancellationToken = default);
}
