namespace Mercurius.LAN.API.Models;

public enum RosterSelectionStatus
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
    public RosterSelectionStatus SelectionStatus { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAtUtc { get; set; }

    public void Confirm(DateTime confirmedAtUtc)
    {
        SelectionStatus = RosterSelectionStatus.Confirmed;
        ConfirmedAtUtc = confirmedAtUtc;
        UpdatedAtUtc = confirmedAtUtc;
    }
}
