using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Services.ParticipantServices;

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

