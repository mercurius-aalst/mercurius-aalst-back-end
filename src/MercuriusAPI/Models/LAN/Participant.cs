namespace MercuriusAPI.Models.LAN
{
    public abstract class Participant
    {
        public int Id { get; set; }
        public string PictureUrl { get; set; } = string.Empty;
        public IList<Game> Games { get; set; } = [];
    }
}
