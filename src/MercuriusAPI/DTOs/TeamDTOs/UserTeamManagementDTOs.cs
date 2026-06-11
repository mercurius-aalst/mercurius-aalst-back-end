using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.TeamDTOs;

public class CurrentUserTeamSummaryDTO
{
    public IEnumerable<TeamManagementSummaryDTO> CaptainedTeams { get; set; } = [];
    public IEnumerable<TeamManagementSummaryDTO> MemberTeams { get; set; } = [];
    public IEnumerable<TeamInviteSummaryDTO> ReceivedPendingInvites { get; set; } = [];
    public IEnumerable<TeamInviteSummaryDTO> SentPendingInvites { get; set; } = [];
}

public class TeamManagementSummaryDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CaptainUserId { get; set; }
    public string? CaptainUsername { get; set; }
    public string? LogoUrl { get; set; }
    public IEnumerable<PublicUserDTO> Members { get; set; } = [];

    public TeamManagementSummaryDTO()
    {
    }

    public TeamManagementSummaryDTO(Team team)
    {
        Id = team.Id;
        Name = team.Name;
        CaptainUserId = team.CaptainUserId ?? Guid.Empty;
        CaptainUsername = team.Captain?.Username;
        LogoUrl = team.LogoUrl;
        Members = team.Members.Select(member => new PublicUserDTO(member));
    }
}

public class TeamInviteSummaryDTO
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

    public TeamInviteSummaryDTO()
    {
    }

    public TeamInviteSummaryDTO(TeamInvite invite)
    {
        Id = invite.Id;
        TeamId = invite.TeamId;
        TeamName = invite.Team.Name;
        TeamLogoUrl = invite.Team.LogoUrl;
        UserId = invite.UserId;
        Username = invite.User.Username;
        Status = invite.Status.ToString();
        CreatedAt = invite.CreatedAt;
        ExpiresAt = invite.ExpiresAt;
    }
}

public record TeamLogoResponseDTO(Guid TeamId, string? LogoUrl);
