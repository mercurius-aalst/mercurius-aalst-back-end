using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.ParticipantServices;

public interface IParticipantService
{
    Task<Participant> GetParticipantByIdAsync(int id);
}