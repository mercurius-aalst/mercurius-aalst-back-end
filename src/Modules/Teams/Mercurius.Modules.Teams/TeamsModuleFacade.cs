using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Contracts;
using Mercurius.Modules.Teams.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Teams;

internal sealed class TeamsModuleFacade : ITeamsModule
{
    private readonly ITeamsDbContext _dbContext;
    private readonly ITeamCompetitionReadService _competitionReadService;

    public TeamsModuleFacade(
        ITeamsDbContext dbContext,
        ITeamCompetitionReadService competitionReadService)
    {
        _dbContext = dbContext;
        _competitionReadService = competitionReadService;
    }

    public Task<TeamSummary?> GetTeamSummaryAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Teams
            .AsNoTracking()
            .Where(team => team.Id == teamId.Value)
            .Select(team => new TeamSummary(
                new TeamId(team.Id),
                team.Name,
                team.CaptainUserId.HasValue ? new UserId(team.CaptainUserId.Value) : null,
                team.LogoUrl,
                team.IsDeleted))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TeamRosterSnapshot?> GetTeamRosterSnapshotAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default)
    {
        var teams = await GetTeamRosterSnapshotsAsync([teamId], cancellationToken);
        return teams.GetValueOrDefault(teamId);
    }

    public async Task<IReadOnlyDictionary<TeamId, TeamRosterSnapshot>> GetTeamRosterSnapshotsAsync(
        IReadOnlyCollection<TeamId> teamIds,
        CancellationToken cancellationToken = default)
    {
        if (teamIds.Count == 0)
            return new Dictionary<TeamId, TeamRosterSnapshot>();

        var ids = teamIds.Select(teamId => teamId.Value).Distinct().ToArray();
        var teams = await _dbContext.Teams
            .AsNoTracking()
            .Where(team => ids.Contains(team.Id))
            .Select(team => new
            {
                team.Id,
                team.Name,
                team.CaptainUserId,
                team.LogoUrl,
                team.IsDeleted,
                Members = team.Members
                    .OrderBy(member => member.Username)
                    .Select(member => new
                    {
                        member.Id,
                        member.Username,
                        member.Firstname,
                        member.Lastname,
                        member.IsDeleted
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return teams.ToDictionary(
            team => new TeamId(team.Id),
            team => new TeamRosterSnapshot(
                new TeamId(team.Id),
                team.Name,
                team.CaptainUserId.HasValue ? new UserId(team.CaptainUserId.Value) : null,
                team.LogoUrl,
                team.IsDeleted,
                team.Members
                    .Select(member => new TeamMemberSnapshot(
                        new UserId(member.Id),
                        member.Username,
                        GetDisplayName(member.Username, member.Firstname, member.Lastname, member.IsDeleted),
                        team.CaptainUserId == member.Id))
                    .ToList()));
    }

    public async Task<PublicTeamProfile?> GetPublicTeamProfileAsync(
        string teamName,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = Domain.Team.NormalizeName(teamName);

        var team = await _dbContext.Teams
            .AsNoTracking()
            .Where(team => team.NormalizedName == normalizedName && !team.IsDeleted)
            .Select(team => new
            {
                team.Id,
                team.Name,
                CaptainUsername = team.Captain == null ? null : team.Captain.Username,
                team.LogoUrl,
                Members = team.Members
                    .Select(member => member.Username)
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (team is null)
            return null;

        var tournaments = await _competitionReadService.GetPublicTeamTournamentsAsync(team.Id, cancellationToken);

        var members = team.Members
            .Where(IsValidPublicUsername)
            .Select(username => username!)
            .OrderBy(username => username, StringComparer.OrdinalIgnoreCase)
            .ThenBy(username => username, StringComparer.Ordinal)
            .Select(username => new PublicTeamMemberSummary(username))
            .ToList();

        return new PublicTeamProfile(
            team.Name,
            IsValidPublicUsername(team.CaptainUsername) ? team.CaptainUsername : null,
            team.LogoUrl,
            members,
            tournaments);
    }

    public async Task<TeamRegistrationEligibility> GetRegistrationEligibilityAsync(
        TeamId teamId,
        UserId requestedBy,
        GameId gameId,
        CancellationToken cancellationToken = default)
    {
        var team = await _dbContext.Teams
            .AsNoTracking()
            .Where(team => team.Id == teamId.Value)
            .Select(team => new
            {
                team.IsDeleted,
                team.CaptainUserId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (team is null)
            return new TeamRegistrationEligibility(false, ["team_not_found"]);

        var reasons = new List<string>();
        if (team.IsDeleted)
            reasons.Add("team_deleted");
        if (team.CaptainUserId != requestedBy.Value)
            reasons.Add("captain_required");

        return new TeamRegistrationEligibility(reasons.Count == 0, reasons);
    }

    public async Task<MembershipMutationGuard> CanMutateMembershipAsync(
        TeamId teamId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var team = await _dbContext.Teams
            .AsNoTracking()
            .Where(team => team.Id == teamId.Value)
            .Select(team => new
            {
                team.IsDeleted,
                team.CaptainUserId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (team is null)
            return new MembershipMutationGuard(false, ["team_not_found"]);

        var reasons = new List<string>();
        if (team.IsDeleted)
            reasons.Add("team_deleted");
        if (team.CaptainUserId != userId.Value)
            reasons.Add("captain_required");

        return new MembershipMutationGuard(reasons.Count == 0, reasons);
    }

    public async Task<IReadOnlyList<PublicTeamSearchDocument>> GetPublicTeamSearchDocumentsPageAsync(
        TeamId? afterId,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var afterValue = afterId?.Value;
        return await _dbContext.Teams
            .AsNoTracking()
            .Where(team =>
                !team.IsDeleted &&
                !string.IsNullOrWhiteSpace(team.Name) &&
                (!afterValue.HasValue || team.Id > afterValue.Value))
            .OrderBy(team => team.Id)
            .Select(team => new PublicTeamSearchDocument(
                new TeamId(team.Id),
                team.Name))
            .Take(Math.Clamp(pageSize, 1, 1000))
            .ToListAsync(cancellationToken);
    }

    private static bool IsValidPublicUsername(string? username)
    {
        return !string.IsNullOrWhiteSpace(username);
    }

    private static string GetDisplayName(
        string? username,
        string? firstname,
        string? lastname,
        bool isDeleted)
    {
        if (isDeleted)
            return "Deleted user";

        var fullName = $"{firstname} {lastname}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return string.IsNullOrWhiteSpace(username) ? "Incomplete profile" : username;
    }
}
