namespace MercuriusAPI.Models.LAN
{
    public class Team: Participant{
        public string Name { get; set; }
        public int CaptainId { get; set; }
        public Player Captain { get; set; }

        public IList<Player> Players { get; set; } = new List<Player>();

        public Team()
        {
            
        }

        public Team(string name, Player captain)
        {
            Name = name;
            Captain = captain;

        }
    }
}
