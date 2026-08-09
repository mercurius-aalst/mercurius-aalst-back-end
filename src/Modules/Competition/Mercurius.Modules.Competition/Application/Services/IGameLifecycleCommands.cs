using Mercurius.Modules.Competition.Application.DTOs.Placements;

namespace Mercurius.Modules.Competition.Application.Services;

internal interface IGameLifecycleCommands
{
    Task CancelGameAsync(Guid id, CancellationToken cancellationToken = default);

    Task StartGameAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<GetPlacementDTO>> CompleteGameAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task ResetGameAsync(Guid id, CancellationToken cancellationToken = default);
}
