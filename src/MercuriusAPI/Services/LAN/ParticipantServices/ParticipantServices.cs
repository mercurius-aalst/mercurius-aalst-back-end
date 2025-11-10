using MercuriusAPI.Data;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.LAN;
using Microsoft.EntityFrameworkCore;

namespace MercuriusAPI.Services.LAN.ParticipantServices;

public class ParticipantService : IParticipantService
{
    private readonly MercuriusDBContext _context;
    public ParticipantService(MercuriusDBContext context)
    {
        _context = context;
    }
    public async Task<Participant> GetParticipantByIdAsync(int id)
    {
        var participant = await _context.Participants
            .FirstOrDefaultAsync(p => p.Id == id);
        if (participant == null)
        {
            throw new NotFoundException($"{nameof(Participant)} not found");
        }
        return participant;
    }
}
