namespace Mercurius.Modules.Competition.Domain;

internal enum RosterMemberConfirmationStatus
{
    AutoConfirmed,
    Pending,
    Confirmed
}

internal sealed class TournamentRegistrationRosterMember
{
    public Guid Id { get; set; }
    public Guid TournamentRegistrationId { get; set; }
    public TournamentRegistration TournamentRegistration { get; set; } = null!;
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    public Guid UserId { get; set; }
    public string UsernameAtRegistration { get; set; } = string.Empty;
    public string DisplayNameAtRegistration { get; set; } = string.Empty;
    public Guid? TeamId { get; set; }
    public string? TeamNameAtRegistration { get; set; }
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
