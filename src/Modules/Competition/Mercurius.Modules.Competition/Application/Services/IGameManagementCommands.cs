using Mercurius.Modules.Competition.Application.DTOs.Games;

namespace Mercurius.Modules.Competition.Application.Services;

internal interface IGameManagementCommands
{
    Task<GetGameDTO> CreateGameAsync(
        CreateGameDTO createGameDTO,
        CancellationToken cancellationToken = default);

    Task<GetGameDTO> UpdateGameAsync(
        Guid id,
        UpdateGameDTO gameDTO,
        CancellationToken cancellationToken = default);

    Task DeleteGameAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GetGameDTO> ReplaceSponsorPlacementsAsync(
        Guid id,
        ReplaceGameSponsorsDTO sponsorDTO,
        CancellationToken cancellationToken = default);
}
