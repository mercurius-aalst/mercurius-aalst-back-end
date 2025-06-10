namespace MercuriusAPI.Models.LAN
{
    public abstract class Participant
    {
        public int Id { get; set; }
        public IList<Game> Games { get; set; } = [];
    }
}
