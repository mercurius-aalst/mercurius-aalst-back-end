namespace Mercurius.LAN.API.DTOs.Public;

public class PublicTeamDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CaptainUserId { get; set; }
    public IEnumerable<PublicParticipantDTO> Members { get; set; } = [];
}
