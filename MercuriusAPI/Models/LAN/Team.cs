using MercuriusAPI.Exceptions;

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
            CaptainId = captain.Id;
            Players.Add(captain);
        }

        public void Update(string? name, int? captainId)
        {
            if(name is not null)
                Name = name;
            if(captainId is not null)
            CaptainId = (int)captainId;
        }

        public void RemovePlayer(int playerId)
        {
            var player = Players.FirstOrDefault(m => m.Id == playerId);
            if(player is null)
                throw new NotFoundException($"{nameof(Player)} not found in {Name}");
            if(player.Id == CaptainId)
                throw new ValidationException("The captain cannot be removed from a team");
            Players.Remove(player);
        }
    }
}
