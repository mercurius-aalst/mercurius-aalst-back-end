using Mercurius.LAN.API.Exceptions;

namespace Mercurius.LAN.API.Models;

public enum TeamInviteStatus
{
    Pending,
    Accepted,
    Declined,
    Cancelled,
    Expired
}

public class TeamInvite
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Team Team { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public TeamInviteStatus Status { get; set; } = TeamInviteStatus.Pending;
    public DateTime? RespondedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ExpiredAt { get; set; }

    public void Respond(bool accept)
    {
        if (Status != TeamInviteStatus.Pending)
            throw new ValidationException("Cannot respond to an invite that is not pending.");
        if (ExpiresAt <= DateTime.UtcNow)
        {
            Expire();
            throw new ValidationException("Cannot respond to an expired invite.");
        }

        Status = accept ? TeamInviteStatus.Accepted : TeamInviteStatus.Declined;
        if (accept)
            Team.Members.Add(User);
        RespondedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != TeamInviteStatus.Pending)
            throw new ValidationException("Cannot cancel an invite that is not pending.");

        Status = TeamInviteStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status != TeamInviteStatus.Pending)
            return;

        Status = TeamInviteStatus.Expired;
        ExpiredAt = DateTime.UtcNow;
    }
}
