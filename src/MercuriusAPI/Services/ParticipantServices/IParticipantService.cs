using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.ParticipantServices;

public interface IParticipantService
{
    Task<Participant> GetParticipantByIdAsync(int id);
}
