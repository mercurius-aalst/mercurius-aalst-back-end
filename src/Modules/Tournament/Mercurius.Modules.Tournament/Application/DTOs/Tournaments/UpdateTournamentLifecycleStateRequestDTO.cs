using Mercurius.Modules.Tournament.Contracts;

namespace Mercurius.Modules.Tournament.Application.DTOs.Tournaments;

internal sealed class UpdateTournamentLifecycleStateRequestDTO
{
    public TournamentStatus? State { get; set; }
}
