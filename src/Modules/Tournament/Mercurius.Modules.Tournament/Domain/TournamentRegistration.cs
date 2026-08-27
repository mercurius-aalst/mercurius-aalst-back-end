namespace Mercurius.Modules.Tournament.Domain;

internal enum TournamentRegistrationKind
{
    Individual,
    Team
}

internal enum TournamentRegistrationStatus
{
    PendingConfirmation,
    Active
}

internal sealed class TournamentRegistration
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentAggregate Tournament { get; set; } = null!;
    public TournamentRegistrationKind Kind { get; set; }
    public TournamentRegistrationStatus Status { get; set; }
    public Guid RegisteredByUserId { get; set; }
    public string RegisteredByUsernameAtRegistration { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UsernameAtRegistration { get; set; }
    public Guid? TeamId { get; set; }
    public string? TeamNameAtRegistration { get; set; }
    public Guid? TeamCaptainUserIdAtRegistration { get; set; }
    public string? TeamLogoUrlAtRegistration { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public IList<TournamentRegistrationRosterMember> RosterMembers { get; set; } = [];

    public void Activate(DateTime updatedAtUtc)
    {
        Status = TournamentRegistrationStatus.Active;
        UpdatedAtUtc = updatedAtUtc;
    }
}
