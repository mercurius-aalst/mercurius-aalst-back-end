namespace Mercurius.LAN.API.Models;

public class TournamentRosterConfirmationNotification
{
    public Guid Id { get; set; }
    public Guid TournamentRegistrationRosterMemberId { get; set; }
    public TournamentRegistrationRosterMember RosterMember { get; set; }
    public Guid TeamId { get; set; }
    public Team Team { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
}
