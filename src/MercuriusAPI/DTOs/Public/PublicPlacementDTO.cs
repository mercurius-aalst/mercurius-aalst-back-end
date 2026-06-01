namespace Mercurius.LAN.API.DTOs.Public;

public class PublicPlacementDTO
{
    public int Place { get; set; }
    public IEnumerable<PublicParticipantDTO> Users { get; set; } = [];
    public IEnumerable<PublicTeamDTO> Teams { get; set; } = [];
}
