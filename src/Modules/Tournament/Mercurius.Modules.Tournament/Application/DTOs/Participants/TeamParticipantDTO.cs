namespace Mercurius.Modules.Tournament.Application.DTOs.Participants;

internal class TeamParticipantDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CaptainUserId { get; set; }
    public string? LogoUrl { get; set; }
    public IEnumerable<PublicUserDTO> Members { get; set; } = [];

    public TeamParticipantDTO()
    {
    }

}
