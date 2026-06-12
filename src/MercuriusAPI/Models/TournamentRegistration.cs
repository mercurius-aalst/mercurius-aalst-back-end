namespace Mercurius.LAN.API.Models;

public enum TournamentRegistrationKind
{
    Individual,
    Team
}

public enum TournamentRegistrationStatus
{
    PendingConfirmation,
    Active
}

public class TournamentRegistration
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Game Game { get; set; }
    public TournamentRegistrationKind Kind { get; set; }
    public TournamentRegistrationStatus Status { get; set; }
    public Guid RegisteredByUserId { get; set; }
    public User RegisteredByUser { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public IList<TournamentRegistrationRosterMember> RosterMembers { get; set; } = [];

    public void Activate(DateTime updatedAtUtc)
    {
        Status = TournamentRegistrationStatus.Active;
        UpdatedAtUtc = updatedAtUtc;
    }
}
