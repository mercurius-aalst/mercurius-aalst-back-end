using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Teams.Api;

public sealed class CreateTeamRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public Guid CaptainUserId { get; set; }
}

public sealed class RespondTeamInviteRequest
{
    [Required]
    public bool Accept { get; set; }
}

public sealed class TransferCaptainRequest
{
    [Required]
    public Guid NewCaptainUserId { get; set; }
}

public sealed class PublicUserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? DiscordId { get; set; }
    public string? SteamId { get; set; }
    public string? RiotId { get; set; }
}

public sealed class TeamResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CaptainUserId { get; set; }
    public string? LogoUrl { get; set; }
    public IEnumerable<PublicUserResponse> Members { get; set; } = [];
}

public sealed class CurrentUserTeamSummaryResponse
{
    public IEnumerable<TeamManagementSummaryResponse> CaptainedTeams { get; set; } = [];
    public IEnumerable<TeamManagementSummaryResponse> MemberTeams { get; set; } = [];
    public IEnumerable<TeamInviteSummaryResponse> ReceivedPendingInvites { get; set; } = [];
    public IEnumerable<TeamInviteSummaryResponse> SentPendingInvites { get; set; } = [];
}

public sealed class TeamManagementSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CaptainUserId { get; set; }
    public string? CaptainUsername { get; set; }
    public string? LogoUrl { get; set; }
    public IEnumerable<PublicUserResponse> Members { get; set; } = [];
}

public sealed class TeamInviteResponse
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
}

public sealed class TeamInviteSummaryResponse
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string? TeamLogoUrl { get; set; }
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public sealed record TeamLogoResponse(Guid TeamId, string? LogoUrl);

public sealed class PublicTeamProfileResponse
{
    public string TeamName { get; set; } = string.Empty;
    public string? CaptainUsername { get; set; }
    public string? LogoUrl { get; set; }
    public IEnumerable<PublicTeamMemberResponse> Members { get; set; } = [];
    public IEnumerable<PublicTeamTournamentResponse> Tournaments { get; set; } = [];
}

public sealed class PublicTeamMemberResponse
{
    public string Username { get; set; } = string.Empty;
}

public sealed class PublicTeamTournamentResponse
{
    public Guid GameId { get; set; }
    public string Name { get; set; } = string.Empty;
}
