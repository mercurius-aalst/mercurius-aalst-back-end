namespace MercuriusAPI.Models.LAN;

public class Placement
{
    public int Id { get; set; }
    public IEnumerable<Participant> Participants { get; set; } = new List<Participant>();
    public int Place { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; }
}
