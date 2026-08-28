using Mercurius.Modules.Tournament.Application.DTOs.Placements;

namespace Mercurius.Modules.Tournament.Application.Services;

internal interface ITournamentLifecycleCommands
{
    Task CancelTournamentAsync(Guid id, CancellationToken cancellationToken = default);

    Task StartTournamentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<GetPlacementDTO>> CompleteTournamentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task ResetTournamentAsync(Guid id, CancellationToken cancellationToken = default);
}
