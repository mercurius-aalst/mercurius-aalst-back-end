using Mercurius.Modules.Tournament.Application.DTOs.Tournaments;
using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Tournament.Application.Services;

internal sealed class FeaturedHomepageTournamentService(ITournamentDbContext dbContext) : IFeaturedHomepageTournamentService
{
    private const int FeaturedTournamentCount = 4;

    public async Task<FeaturedHomepageTournamentsDTO> GetFeaturedTournamentsAsync(
        CancellationToken cancellationToken = default)
    {
        var tournamentIds = await dbContext.FeaturedHomepageTournamentSelections
            .AsNoTracking()
            .Where(selection => selection.Id == FeaturedHomepageTournamentSelection.SingletonId)
            .Select(selection => selection.TournamentIds)
            .SingleOrDefaultAsync(cancellationToken);

        if (tournamentIds is null)
        {
            var fallback = await GetEligibleTournamentQuery()
                .OrderBy(tournament => tournament.PlannedStartTime)
                .ThenBy(tournament => tournament.Name)
                .ThenBy(tournament => tournament.Id)
                .Take(FeaturedTournamentCount)
                .Select(ToCard())
                .ToListAsync(cancellationToken);
            return CreateResponse(fallback);
        }

        var selected = await GetEligibleTournamentQuery()
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .Select(ToCard())
            .ToListAsync(cancellationToken);
        var selectedById = selected.ToDictionary(tournament => tournament.Id);
        var ordered = tournamentIds
            .Distinct()
            .Where(selectedById.ContainsKey)
            .Select(id => selectedById[id])
            .ToList();
        var vacancyCount = FeaturedTournamentCount - ordered.Count;

        if (vacancyCount > 0)
        {
            ordered.AddRange(await GetEligibleTournamentQuery()
                .Where(tournament => !tournamentIds.Contains(tournament.Id))
                .OrderBy(tournament => tournament.PlannedStartTime)
                .ThenBy(tournament => tournament.Name)
                .ThenBy(tournament => tournament.Id)
                .Take(vacancyCount)
                .Select(ToCard())
                .ToListAsync(cancellationToken));
        }

        return CreateResponse(ordered);
    }

    public async Task<Dictionary<string, string[]>?> ReplaceFeaturedTournamentsAsync(
        Guid[]? tournamentIds,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();
        if (tournamentIds is null || tournamentIds.Length != FeaturedTournamentCount)
        {
            errors["tournamentIds"] = ["Exactly four tournament IDs are required."];
            return errors;
        }

        if (tournamentIds.Distinct().Count() != FeaturedTournamentCount)
        {
            errors["tournamentIds"] = ["Tournament IDs must be distinct."];
            return errors;
        }

        if (await GetEligibleTournamentQuery().CountAsync(cancellationToken) < FeaturedTournamentCount)
        {
            errors["tournamentIds"] = ["At least four eligible tournaments must exist before featured tournaments can be saved."];
            return errors;
        }

        var eligibleIds = await GetEligibleTournamentQuery()
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .Select(tournament => tournament.Id)
            .ToListAsync(cancellationToken);
        if (eligibleIds.Count != FeaturedTournamentCount)
        {
            var invalidIds = tournamentIds.Except(eligibleIds);
            errors["tournamentIds"] = [$"Every ID must identify an existing non-canceled tournament. Invalid IDs: {string.Join(", ", invalidIds)}."];
            return errors;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tournament.featured_homepage_tournaments ("Id", "TournamentIds")
            VALUES ({FeaturedHomepageTournamentSelection.SingletonId}, {tournamentIds})
            ON CONFLICT ("Id")
            DO UPDATE SET "TournamentIds" = EXCLUDED."TournamentIds"
            """, cancellationToken);

        return null;
    }

    private IQueryable<TournamentAggregate> GetEligibleTournamentQuery() => dbContext.Tournaments
        .AsNoTracking()
        .Where(tournament => tournament.Status != TournamentStatus.Canceled);

    private static System.Linq.Expressions.Expression<Func<TournamentAggregate, FeaturedHomepageTournamentCardDTO>> ToCard() =>
        tournament => new FeaturedHomepageTournamentCardDTO(
            tournament.Id,
            tournament.Name,
            tournament.ImageUrl,
            (Contracts.TournamentStatus)tournament.Status,
            (Contracts.BracketType)tournament.BracketType,
            (Contracts.GameFormat)tournament.Format);

    private static FeaturedHomepageTournamentsDTO CreateResponse(
        IReadOnlyList<FeaturedHomepageTournamentCardDTO> tournaments) =>
        new(tournaments.Select(tournament => tournament.Id).ToArray(), tournaments);
}
