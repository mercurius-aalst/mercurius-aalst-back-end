using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.Modules.Teams.Api;

namespace Mercurius.LAN.API.Services.TeamServices;

public sealed class TeamsEndpointServiceAdapter : ITeamsEndpointService
{
    private readonly ITeamService _teamService;

    public TeamsEndpointServiceAdapter(ITeamService teamService)
    {
        _teamService = teamService;
    }

    public async Task<IEnumerable<TeamResponse>> GetAllTeamsAsync(CancellationToken cancellationToken = default)
    {
        var teams = await _teamService.GetAllTeamsAsync(cancellationToken);
        return teams.Select(MapTeam);
    }

    public async Task<TeamResponse> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return MapTeam(await _teamService.GetTeamByIdAsync(teamId, cancellationToken));
    }

    public async Task<TeamManagementSummaryResponse> CreateCurrentUserTeamAsync(string auth0UserId, CreateTeamRequest request)
    {
        var team = await _teamService.CreateCurrentUserTeamAsync(
            auth0UserId,
            new CreateTeamDTO
            {
                Name = request.Name,
                CaptainUserId = request.CaptainUserId
            });

        return MapManagementSummary(team);
    }

    public async Task<CurrentUserTeamSummaryResponse> GetCurrentUserTeamSummaryAsync(string auth0UserId)
    {
        var summary = await _teamService.GetCurrentUserTeamSummaryAsync(auth0UserId);

        return new CurrentUserTeamSummaryResponse
        {
            CaptainedTeams = summary.CaptainedTeams.Select(MapManagementSummary),
            MemberTeams = summary.MemberTeams.Select(MapManagementSummary),
            ReceivedPendingInvites = summary.ReceivedPendingInvites.Select(MapInviteSummary),
            SentPendingInvites = summary.SentPendingInvites.Select(MapInviteSummary)
        };
    }

    public async Task<IEnumerable<TeamInviteSummaryResponse>> GetCurrentUserInvitesAsync(string auth0UserId)
    {
        var invites = await _teamService.GetCurrentUserInvitesAsync(auth0UserId);
        return invites.Select(MapInviteSummary);
    }

    public async Task<IEnumerable<TeamInviteSummaryResponse>> GetCurrentUserSentInvitesAsync(string auth0UserId)
    {
        var invites = await _teamService.GetCurrentUserSentInvitesAsync(auth0UserId);
        return invites.Select(MapInviteSummary);
    }

    public async Task<TeamManagementSummaryResponse> LeaveTeamAsync(string auth0UserId, Guid teamId)
    {
        return MapManagementSummary(await _teamService.LeaveTeamAsync(auth0UserId, teamId));
    }

    public async Task<TeamManagementSummaryResponse> RemoveMemberAsync(string auth0UserId, Guid teamId, Guid userId)
    {
        return MapManagementSummary(await _teamService.RemoveMemberAsync(auth0UserId, teamId, userId));
    }

    public Task DeleteTeamAsync(string auth0UserId, Guid teamId)
    {
        return _teamService.DeleteTeamAsync(auth0UserId, teamId);
    }

    public async Task<TeamInviteResponse> InviteUserAsync(string auth0UserId, Guid teamId, Guid userId)
    {
        return MapInvite(await _teamService.InviteUserAsync(auth0UserId, teamId, userId));
    }

    public async Task<TeamInviteResponse> CancelInviteAsync(string auth0UserId, Guid teamId, Guid inviteId)
    {
        return MapInvite(await _teamService.CancelInviteAsync(auth0UserId, teamId, inviteId));
    }

    public async Task<TeamInviteResponse> RespondToInviteAsync(string auth0UserId, Guid inviteId, bool accept)
    {
        return MapInvite(await _teamService.RespondToInviteAsync(auth0UserId, inviteId, accept));
    }

    public async Task<TeamManagementSummaryResponse> TransferCaptainAsync(string auth0UserId, Guid teamId, Guid newCaptainUserId)
    {
        return MapManagementSummary(await _teamService.TransferCaptainAsync(auth0UserId, teamId, newCaptainUserId));
    }

    public async Task<TeamLogoResponse> UploadTeamLogoAsync(string auth0UserId, Guid teamId, IFormFile logo)
    {
        var logoResponse = await _teamService.UploadTeamLogoAsync(auth0UserId, teamId, logo);
        return new TeamLogoResponse(logoResponse.TeamId, logoResponse.LogoUrl);
    }

    public async Task<TeamLogoResponse> RemoveTeamLogoAsync(string auth0UserId, Guid teamId)
    {
        var logoResponse = await _teamService.RemoveTeamLogoAsync(auth0UserId, teamId);
        return new TeamLogoResponse(logoResponse.TeamId, logoResponse.LogoUrl);
    }

    public async Task<PublicTeamProfileResponse> GetPublicTeamProfileAsync(string teamName)
    {
        var profile = await _teamService.GetPublicTeamProfileAsync(teamName);

        return new PublicTeamProfileResponse
        {
            TeamName = profile.TeamName,
            CaptainUsername = profile.CaptainUsername,
            LogoUrl = profile.LogoUrl,
            Members = profile.Members.Select(member => new PublicTeamMemberResponse
            {
                Username = member.Username
            }),
            Tournaments = profile.Tournaments.Select(tournament => new PublicTeamTournamentResponse
            {
                GameId = tournament.GameId,
                Name = tournament.Name
            })
        };
    }

    private static TeamResponse MapTeam(GetTeamDTO team)
    {
        return new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            CaptainUserId = team.CaptainUserId,
            LogoUrl = team.LogoUrl,
            Members = team.Members.Select(MapPublicUser)
        };
    }

    private static TeamManagementSummaryResponse MapManagementSummary(TeamManagementSummaryDTO team)
    {
        return new TeamManagementSummaryResponse
        {
            Id = team.Id,
            Name = team.Name,
            CaptainUserId = team.CaptainUserId,
            CaptainUsername = team.CaptainUsername,
            LogoUrl = team.LogoUrl,
            Members = team.Members.Select(MapPublicUser)
        };
    }

    private static TeamInviteResponse MapInvite(TeamInviteDTO invite)
    {
        return new TeamInviteResponse
        {
            Id = invite.Id,
            TeamId = invite.TeamId,
            UserId = invite.UserId,
            Status = invite.Status,
            CreatedAt = invite.CreatedAt,
            ExpiresAt = invite.ExpiresAt,
            RespondedAt = invite.RespondedAt,
            CancelledAt = invite.CancelledAt,
            ExpiredAt = invite.ExpiredAt
        };
    }

    private static TeamInviteSummaryResponse MapInviteSummary(TeamInviteSummaryDTO invite)
    {
        return new TeamInviteSummaryResponse
        {
            Id = invite.Id,
            TeamId = invite.TeamId,
            TeamName = invite.TeamName,
            TeamLogoUrl = invite.TeamLogoUrl,
            UserId = invite.UserId,
            Username = invite.Username,
            Status = invite.Status,
            CreatedAt = invite.CreatedAt,
            ExpiresAt = invite.ExpiresAt
        };
    }

    private static PublicUserResponse MapPublicUser(PublicUserDTO user)
    {
        return new PublicUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            DiscordId = user.DiscordId,
            SteamId = user.SteamId,
            RiotId = user.RiotId
        };
    }
}
