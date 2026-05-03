namespace Mercurius.LAN.API.Models;

public class Placement
{
    public Guid Id { get; set; }
    public IEnumerable<User> Users { get; set; } = new List<User>();
    public IEnumerable<Team> Teams { get; set; } = new List<Team>();
    public int Place { get; set; }
    public Guid GameId { get; set; }
    public Game Game { get; set; }
}

