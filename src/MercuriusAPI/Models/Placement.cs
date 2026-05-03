namespace Mercurius.LAN.API.Models;

public class Placement
{
    public int Id { get; set; }
    public IEnumerable<User> Users { get; set; } = new List<User>();
    public IEnumerable<Team> Teams { get; set; } = new List<Team>();
    public int Place { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; }
}

