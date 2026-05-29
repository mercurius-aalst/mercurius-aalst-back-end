namespace Mercurius.LAN.API.DTOs.TeamDTOs;

public class PublicTeamMemberDTO
{
    public string Username { get; set; } = string.Empty;

    public PublicTeamMemberDTO()
    {
    }

    public PublicTeamMemberDTO(string username)
    {
        Username = username;
    }
}
