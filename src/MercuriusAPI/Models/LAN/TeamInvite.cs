using System;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Models.LAN
{
    public enum TeamInviteStatus
    {
        Pending,
        Accepted,
        Declined
    }

    public class TeamInvite
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public Team Team { get; set; }
        public int PlayerId { get; set; }
        public Player Player { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TeamInviteStatus Status { get; set; } = TeamInviteStatus.Pending;
        public DateTime? RespondedAt { get; set; }

        public void Respond(bool accept)
        {
            if(Status != TeamInviteStatus.Pending)
                throw new ValidationException("Cannot respond to an invite that is not pending.");

            Status = accept ? TeamInviteStatus.Accepted : TeamInviteStatus.Declined;
            if(accept)
                Team.Players.Add(Player);
            RespondedAt = DateTime.UtcNow;
        }
    }
}