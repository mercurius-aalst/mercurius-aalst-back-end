using Mercurius.Modules.Competition.Application;
using Mercurius.Modules.Competition.Contracts;
using Mercurius.Modules.Competition.Domain;
using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Mercurius.Modules.Competition;

internal sealed class CompetitionModuleFacade : ICompetitionModule
{
    private readonly ICompetitionDbContext _dbContext;
    private readonly ITeamsModule _teamsModule;
    private readonly CompetitionEligibilityEvaluator _eligibilityEvaluator;

    public CompetitionModuleFacade(
        ICompetitionDbContext dbContext,
        ITeamsModule teamsModule,
        CompetitionEligibilityEvaluator eligibilityEvaluator)
    {
        _dbContext = dbContext;
        _teamsModule = teamsModule;
        _eligibilityEvaluator = eligibilityEvaluator;
    }

    public Task<GameSummary?> GetGameSummaryAsync(
        GameId gameId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Id == gameId.Value)
            .Select(game => new GameSummary(
                new GameId(game.Id),
                game.Name,
                (Contracts.GameStatus)game.Status,
                (Contracts.ParticipationMode)game.ParticipationMode,
                game.TeamSize,
                game.PlannedStartTime,
                game.ImageUrl))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<TournamentConfiguration?> GetTournamentConfigurationAsync(
        GameId gameId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Id == gameId.Value)
            .Select(game => new TournamentConfiguration(
                new GameId(game.Id),
                (Contracts.BracketType)game.BracketType,
                (Contracts.GameFormat)game.Format,
                (Contracts.GameFormat)game.FinalsFormat,
                (Contracts.ParticipationMode)game.ParticipationMode,
                game.TeamSize,
                game.PlannedStartTime,
                game.AverageGameDurationMinutes,
                game.RoundBreakDurationMinutes))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsRegistrationOpenAsync(
        GameId gameId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Games
            .AsNoTracking()
            .AnyAsync(
                game => game.Id == gameId.Value && game.Status == Domain.GameStatus.Scheduled,
                cancellationToken);
    }

    public async Task<RegistrationEligibility> CheckIndividualRegistrationEligibilityAsync(
        GameId gameId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var game = await _dbContext.Games
            .AsNoTracking()
            .Where(candidate => candidate.Id == gameId.Value)
            .Select(candidate => new { candidate.Status, candidate.ParticipationMode })
            .SingleOrDefaultAsync(cancellationToken);
        if (game is null)
            return new RegistrationEligibility(false, ["game_not_found"]);

        var reasons = await _eligibilityEvaluator.GetIndividualCompetitionFailuresAsync(
            new Game
            {
                Id = gameId.Value,
                ParticipationMode = game.ParticipationMode,
                Status = game.Status
            },
            userId.Value,
            null,
            cancellationToken);
        return new RegistrationEligibility(reasons.Count == 0, reasons);
    }

    public async Task<RegistrationEligibility> CheckTeamRegistrationEligibilityAsync(
        GameId gameId,
        TeamId teamId,
        UserId requestedBy,
        CancellationToken cancellationToken = default)
    {
        var game = await _dbContext.Games
            .AsNoTracking()
            .Where(candidate => candidate.Id == gameId.Value)
            .Select(candidate => new
            {
                candidate.Status,
                candidate.ParticipationMode,
                candidate.TeamSize
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (game is null)
            return new RegistrationEligibility(false, ["game_not_found"]);

        var teamEligibility = await _teamsModule.GetRegistrationEligibilityAsync(
            teamId,
            requestedBy,
            gameId,
            cancellationToken);
        var reasons = teamEligibility.ReasonCodes.ToList();
        reasons.AddRange(await _eligibilityEvaluator.GetTeamCompetitionFailuresAsync(
            new Game
            {
                Id = gameId.Value,
                ParticipationMode = game.ParticipationMode,
                Status = game.Status,
                TeamSize = game.TeamSize
            },
            teamId.Value,
            requestedBy.Value,
            null,
            cancellationToken));

        return new RegistrationEligibility(reasons.Count == 0, reasons.Distinct().ToList());
    }

    public async Task<IReadOnlyList<GameSummary>> SearchGamesAsync(
        string normalizedQuery,
        CompetitionSearchCursor? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var boundedLimit = Math.Clamp(limit, 1, 500);
        var query = BuildGameSearchQuery(normalizedQuery);

        if (cursor is not null)
        {
            query = query.Where(candidate =>
                candidate.RelevanceRank > cursor.RelevanceRank ||
                (candidate.RelevanceRank == cursor.RelevanceRank &&
                 string.Compare(candidate.NormalizedLabel, cursor.NormalizedLabel) > 0) ||
                (candidate.RelevanceRank == cursor.RelevanceRank &&
                 candidate.NormalizedLabel == cursor.NormalizedLabel &&
                 2 > cursor.TypeOrder) ||
                (candidate.RelevanceRank == cursor.RelevanceRank &&
                 candidate.NormalizedLabel == cursor.NormalizedLabel &&
                 cursor.TypeOrder == 2 &&
                 candidate.Id.CompareTo(cursor.StableId) > 0));
        }

        return await query
            .OrderBy(candidate => candidate.RelevanceRank)
            .ThenBy(candidate => candidate.NormalizedLabel)
            .ThenBy(candidate => candidate.Id)
            .Take(boundedLimit)
            .Select(candidate => new GameSummary(
                new GameId(candidate.Id),
                candidate.Name,
                (Contracts.GameStatus)candidate.Status,
                (Contracts.ParticipationMode)candidate.ParticipationMode,
                candidate.TeamSize,
                candidate.PlannedStartTime,
                candidate.ImageUrl))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GameSearchDocument>> GetGameSearchDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Games
            .AsNoTracking()
            .OrderBy(game => game.Name)
            .ThenBy(game => game.Id)
            .Select(game => new GameSearchDocument(
                new GameId(game.Id),
                game.Name,
                game.ImageUrl))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<GameSearchCandidate> BuildGameSearchQuery(string normalizedQuery)
    {
        var escapedQuery = EscapeLikePattern(normalizedQuery);
        var exactPattern = escapedQuery;
        var containsPattern = $"%{escapedQuery}%";
        var prefixPattern = $"{escapedQuery}%";

        return _dbContext.Games
            .AsNoTracking()
            .Where(game => EF.Functions.Like(game.Name.ToLower(), containsPattern, "\\"))
            .Select(game => new GameSearchCandidate(
                game.Id,
                game.Name,
                game.Status,
                game.ParticipationMode,
                game.TeamSize,
                game.PlannedStartTime,
                game.ImageUrl,
                game.Name.ToLower(),
                EF.Functions.Like(game.Name.ToLower(), exactPattern, "\\")
                    ? 0
                    : EF.Functions.Like(game.Name.ToLower(), prefixPattern, "\\")
                        ? 1
                        : 2));
    }

    private sealed record GameSearchCandidate(
        Guid Id,
        string Name,
        Domain.GameStatus Status,
        Domain.ParticipationMode ParticipationMode,
        int? TeamSize,
        DateTime PlannedStartTime,
        string? ImageUrl,
        string NormalizedLabel,
        int RelevanceRank);

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
