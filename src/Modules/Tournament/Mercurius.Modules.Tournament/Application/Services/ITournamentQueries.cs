using Mercurius.Modules.Tournament.Application.DTOs.Tournaments;

namespace Mercurius.Modules.Tournament.Application.Services;

internal interface ITournamentQueries
{
    Task<GetTournamentDTO> GetTournamentByIdAsync(Guid tournamentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GetTournamentDTO>> GetAllTournamentsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
