using Mercurius.LAN.API.Exceptions;

namespace Mercurius.LAN.API.Models;

public enum TeamInviteStatus
{
    Pending,
    Accepted,
    Declined
}

public class TeamInvite
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Team Team { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TeamInviteStatus Status { get; set; } = TeamInviteStatus.Pending;
    public DateTime? RespondedAt { get; set; }

    public void Respond(bool accept)
    {
        if (Status != TeamInviteStatus.Pending)
            throw new ValidationException("Cannot respond to an invite that is not pending.");

        Status = accept ? TeamInviteStatus.Accepted : TeamInviteStatus.Declined;
        if (accept)
            Team.Members.Add(User);
        RespondedAt = DateTime.UtcNow;
    }
}
