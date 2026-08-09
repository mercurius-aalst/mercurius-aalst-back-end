using Mercurius.Modules.Competition.Contracts;

namespace Mercurius.Modules.Competition.Application.DTOs.Games;

internal sealed class UpdateGameLifecycleStateRequestDTO
{
    public GameStatus? State { get; set; }
}
