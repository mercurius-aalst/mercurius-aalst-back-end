using Mercurius.Modules.Tournament.Application.DTOs.Tournaments;

namespace Mercurius.Modules.Tournament.Application.Services;

internal interface ITournamentManagementCommands
{
    Task<GetTournamentDTO> CreateTournamentAsync(
        CreateTournamentDTO createTournamentDTO,
        CancellationToken cancellationToken = default);

    Task<GetTournamentDTO> UpdateTournamentAsync(
        Guid id,
        UpdateTournamentDTO tournamentDTO,
        CancellationToken cancellationToken = default);

    Task DeleteTournamentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GetTournamentDTO> ReplaceSponsorPlacementsAsync(
        Guid id,
        ReplaceTournamentSponsorsDTO sponsorDTO,
        CancellationToken cancellationToken = default);
}
