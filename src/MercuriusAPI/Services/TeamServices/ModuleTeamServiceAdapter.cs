using Mercurius.LAN.API.DTOs.UserDTOs;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.Modules.Teams.DTOs;
using ModuleTeamLogoResponseDTO = Mercurius.Modules.Teams.DTOs.TeamLogoResponseDTO;
using ModuleTeamService = Mercurius.Modules.Teams.Services.ITeamService;

namespace Mercurius.LAN.API.Services.TeamServices;

public sealed class ModuleTeamServiceAdapter : ModuleTeamService
{
    private readonly ITeamService _teamService;

    public ModuleTeamServiceAdapter(ITeamService teamService)
    {
        _teamService = teamService;
    }

    public async Task<IReadOnlyList<TeamResponseDTO>> GetAllTeamsAsync(CancellationToken cancellationToken = default)
    {
        var teams = await _teamService.GetAllTeamsAsync(cancellationToken);
        return teams.Select(MapTeam).ToList();
    }

    public async Task<TeamResponseDTO> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return MapTeam(await _teamService.GetTeamByIdAsync(teamId, cancellationToken));
    }

    public async Task<TeamManagementSummaryResponseDTO> CreateCurrentUserTeamAsync(
        string auth0UserId,
        CreateTeamRequestDTO request,
        CancellationToken cancellationToken = default)
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

    public async Task<CurrentUserTeamSummaryResponseDTO> GetCurrentUserTeamSummaryAsync(
        string auth0UserId,
        CancellationToken cancellationToken = default)
    {
        var summary = await _teamService.GetCurrentUserTeamSummaryAsync(auth0UserId);

        return new CurrentUserTeamSummaryResponseDTO
        {
            CaptainedTeams = summary.CaptainedTeams.Select(MapManagementSummary).ToList(),
            MemberTeams = summary.MemberTeams.Select(MapManagementSummary).ToList(),
            ReceivedPendingInvites = summary.ReceivedPendingInvites.Select(MapInviteSummary).ToList(),
            SentPendingInvites = summary.SentPendingInvites.Select(MapInviteSummary).ToList()
        };
    }

    public async Task<IReadOnlyList<TeamInviteSummaryResponseDTO>> GetCurrentUserInvitesAsync(
        string auth0UserId,
        CancellationToken cancellationToken = default)
    {
        var invites = await _teamService.GetCurrentUserInvitesAsync(auth0UserId);
        return invites.Select(MapInviteSummary).ToList();
    }

    public async Task<IReadOnlyList<TeamInviteSummaryResponseDTO>> GetCurrentUserSentInvitesAsync(
        string auth0UserId,
        CancellationToken cancellationToken = default)
    {
        var invites = await _teamService.GetCurrentUserSentInvitesAsync(auth0UserId);
        return invites.Select(MapInviteSummary).ToList();
    }

    public async Task<TeamManagementSummaryResponseDTO> LeaveTeamAsync(
        string auth0UserId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return MapManagementSummary(await _teamService.LeaveTeamAsync(auth0UserId, teamId));
    }

    public async Task<TeamManagementSummaryResponseDTO> RemoveMemberAsync(
        string auth0UserId,
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return MapManagementSummary(await _teamService.RemoveMemberAsync(auth0UserId, teamId, userId));
    }

    public Task DeleteTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        return _teamService.DeleteTeamAsync(auth0UserId, teamId);
    }

    public async Task<TeamInviteResponseDTO> InviteUserAsync(
        string auth0UserId,
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return MapInvite(await _teamService.InviteUserAsync(auth0UserId, teamId, userId));
    }

    public async Task<TeamInviteResponseDTO> CancelInviteAsync(
        string auth0UserId,
        Guid teamId,
        Guid inviteId,
        CancellationToken cancellationToken = default)
    {
        return MapInvite(await _teamService.CancelInviteAsync(auth0UserId, teamId, inviteId));
    }

    public async Task<TeamInviteResponseDTO> RespondToInviteAsync(
        string auth0UserId,
        Guid inviteId,
        bool accept,
        CancellationToken cancellationToken = default)
    {
        return MapInvite(await _teamService.RespondToInviteAsync(auth0UserId, inviteId, accept));
    }

    public async Task<TeamManagementSummaryResponseDTO> TransferCaptainAsync(
        string auth0UserId,
        Guid teamId,
        Guid newCaptainUserId,
        CancellationToken cancellationToken = default)
    {
        return MapManagementSummary(await _teamService.TransferCaptainAsync(auth0UserId, teamId, newCaptainUserId));
    }

    public async Task<ModuleTeamLogoResponseDTO> UploadTeamLogoAsync(
        string auth0UserId,
        Guid teamId,
        IFormFile logo,
        CancellationToken cancellationToken = default)
    {
        var logoResponse = await _teamService.UploadTeamLogoAsync(auth0UserId, teamId, logo);
        return new ModuleTeamLogoResponseDTO(logoResponse.TeamId, logoResponse.LogoUrl);
    }

    public async Task<ModuleTeamLogoResponseDTO> RemoveTeamLogoAsync(
        string auth0UserId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var logoResponse = await _teamService.RemoveTeamLogoAsync(auth0UserId, teamId);
        return new ModuleTeamLogoResponseDTO(logoResponse.TeamId, logoResponse.LogoUrl);
    }

    public async Task<PublicTeamProfileResponseDTO> GetPublicTeamProfileAsync(
        string teamName,
        CancellationToken cancellationToken = default)
    {
        var profile = await _teamService.GetPublicTeamProfileAsync(teamName);

        return new PublicTeamProfileResponseDTO
        {
            TeamName = profile.TeamName,
            CaptainUsername = profile.CaptainUsername,
            LogoUrl = profile.LogoUrl,
            Members = profile.Members.Select(member => new PublicTeamMemberResponseDTO
            {
                Username = member.Username
            }).ToList(),
            Tournaments = profile.Tournaments.Select(tournament => new PublicTeamTournamentResponseDTO
            {
                GameId = tournament.GameId,
                Name = tournament.Name
            }).ToList()
        };
    }

    private static TeamResponseDTO MapTeam(GetTeamDTO team)
    {
        return new TeamResponseDTO
        {
            Id = team.Id,
            Name = team.Name,
            CaptainUserId = team.CaptainUserId,
            LogoUrl = team.LogoUrl,
            Members = team.Members.Select(MapPublicUser).ToList()
        };
    }

    private static TeamManagementSummaryResponseDTO MapManagementSummary(TeamManagementSummaryDTO team)
    {
        return new TeamManagementSummaryResponseDTO
        {
            Id = team.Id,
            Name = team.Name,
            CaptainUserId = team.CaptainUserId,
            CaptainUsername = team.CaptainUsername,
            LogoUrl = team.LogoUrl,
            Members = team.Members.Select(MapPublicUser).ToList()
        };
    }

    private static TeamInviteResponseDTO MapInvite(TeamInviteDTO invite)
    {
        return new TeamInviteResponseDTO
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

    private static TeamInviteSummaryResponseDTO MapInviteSummary(TeamInviteSummaryDTO invite)
    {
        return new TeamInviteSummaryResponseDTO
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

    private static PublicUserResponseDTO MapPublicUser(PublicUserDTO user)
    {
        return new PublicUserResponseDTO
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
