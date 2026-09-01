namespace Mercurius.Modules.Tournament.Domain;

internal sealed class MatchResolutionNotification
{
    // The platform event message id is the primary key. This makes a retry
    // idempotent even when the inbox marker was not committed with the row.
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid TournamentId { get; set; }
    public Guid? RecipientUserId { get; set; }
    public MatchResolutionNotificationRecipientKind RecipientKind { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
