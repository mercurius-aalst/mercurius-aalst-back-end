using MercuriusAPI.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MercuriusAPI.Models.LAN
{
    public class Team : Participant
    {
        public string Name { get; set; }
        public int CaptainId { get; set; }
        public Player Captain { get; set; }

        public IList<Player> Players { get; set; } = new List<Player>();
        public IList<TeamInvite> TeamInvites { get; set; } = new List<TeamInvite>();

        public Team()
        {

        }

        public Team(string name, Player captain, string pictureUrl)
        {
            Name = name;
            Captain = captain;
            CaptainId = captain.Id;
            PictureUrl = string.IsNullOrEmpty(pictureUrl) ? "default picture url": pictureUrl ;
            Players.Add(captain);
        }

        public void Update(string name, int captainId, string pictureUrl)
        {
            Name = name;
            CaptainId = captainId;
            PictureUrl = pictureUrl;
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

        public TeamInvite InvitePlayer(int playerId, int inviteResendCooldownDays)
        {
            if(Players.Any(p => p.Id == playerId))
                throw new ValidationException("Player is already in the team");
            if(TeamInvites.Any(i => i.PlayerId == playerId && i.Status == TeamInviteStatus.Pending))
                throw new ValidationException("Player already has a pending invite to this team");
            var lastDeclinedInvite = TeamInvites
               .Where(i => i.PlayerId == playerId && i.Status == TeamInviteStatus.Declined)
               .OrderByDescending(i => i.RespondedAt)
               .FirstOrDefault();
            if(lastDeclinedInvite != null && lastDeclinedInvite.RespondedAt.HasValue)
            {
                var daysSinceDeclined = (DateTime.UtcNow - lastDeclinedInvite.RespondedAt.Value).TotalDays;
                if(daysSinceDeclined < inviteResendCooldownDays)
                {
                    throw new ValidationException($"Player declined the last invite less than {inviteResendCooldownDays} days ago. Please wait {inviteResendCooldownDays - (int)daysSinceDeclined} more day(s).");
                }
            }
            var invite = new TeamInvite { TeamId = Id, PlayerId = playerId };
            TeamInvites.Add(invite);
            return invite;
        }
    }
}
