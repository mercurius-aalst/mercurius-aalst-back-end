using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.Auth;

public class GetUserDTO
{
    public int Id { get; set; }
    public string Username { get; set; }
    public IEnumerable<string> Roles { get; set; }

    public GetUserDTO(User user)
    {
        Id = user.Id;
        Username = user.Username;
        Roles = user.Roles.Select(r => r.Name);
    }
}
