namespace Mercurius.Modules.Tournament.Application.DTOs.Registrations;

internal sealed class RosterConfirmationNotificationDTO
{
    public Guid RosterMemberId { get; set; }
    public Guid TournamentId { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string? TeamLogoUrl { get; set; }
    public DateTime SelectedAtUtc { get; set; }
}

internal sealed class RosterConfirmationNotificationPageDTO
{
    public IReadOnlyList<RosterConfirmationNotificationDTO> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
