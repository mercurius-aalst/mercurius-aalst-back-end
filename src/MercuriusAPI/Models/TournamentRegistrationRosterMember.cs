namespace Mercurius.LAN.API.Models;

public enum RosterMemberConfirmationStatus
{
    AutoConfirmed,
    Pending,
    Confirmed
}

public class TournamentRegistrationRosterMember
{
    public Guid Id { get; set; }
    public Guid TournamentRegistrationId { get; set; }
    public TournamentRegistration TournamentRegistration { get; set; }
    public Guid GameId { get; set; }
    public Game Game { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }
    public bool IsCaptain { get; set; }
    public RosterMemberConfirmationStatus ConfirmationStatus { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAtUtc { get; set; }

    public void Confirm(DateTime confirmedAtUtc)
    {
        ConfirmationStatus = RosterMemberConfirmationStatus.Confirmed;
        ConfirmedAtUtc = confirmedAtUtc;
        UpdatedAtUtc = confirmedAtUtc;
    }
}
