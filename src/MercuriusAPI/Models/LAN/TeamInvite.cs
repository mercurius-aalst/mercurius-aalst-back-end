using System;
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
    }
}