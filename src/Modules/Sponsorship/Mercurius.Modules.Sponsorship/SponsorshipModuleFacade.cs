using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Sponsorship.Application;
using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.Modules.Sponsorship.Contracts.V1;
using Mercurius.Modules.Sponsorship.Domain;
using Mercurius.Modules.Sponsorship.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Sponsorship;

internal sealed class SponsorshipModuleFacade : ISponsorshipModule
{
    private readonly ISponsorshipDbContext _dbContext;
    private readonly SponsorshipOutboxWriter _outboxWriter;

    public SponsorshipModuleFacade(
        ISponsorshipDbContext dbContext,
        SponsorshipOutboxWriter outboxWriter)
    {
        _dbContext = dbContext;
        _outboxWriter = outboxWriter;
    }

    public Task<SponsorSummary?> GetSponsorSummaryAsync(
        SponsorId sponsorId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Sponsors
            .AsNoTracking()
            .Where(sponsor => sponsor.Id == sponsorId.Value)
            .Select(sponsor => new SponsorSummary(
                new SponsorId(sponsor.Id),
                sponsor.Name,
                sponsor.SponsorTier,
                sponsor.LogoUrl,
                sponsor.InfoUrl,
                sponsor.Description))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SponsorSummary>> GetSponsorsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sponsors
            .AsNoTracking()
            .OrderBy(sponsor => sponsor.SponsorTier)
            .ThenBy(sponsor => sponsor.Name)
            .Select(sponsor => new SponsorSummary(
                new SponsorId(sponsor.Id),
                sponsor.Name,
                sponsor.SponsorTier,
                sponsor.LogoUrl,
                sponsor.InfoUrl,
                sponsor.Description))
            .ToListAsync(cancellationToken);
    }

    public Task<SponsorPlacementSummary?> GetSponsorPlacementAsync(
        GameId gameId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.GameSponsorPlacements
            .AsNoTracking()
            .Where(placement => placement.GameId == gameId.Value)
            .Select(ToPlacementSummary())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<GameId, SponsorPlacementSummary>> GetSponsorPlacementsAsync(
        IReadOnlyCollection<GameId> gameIds,
        CancellationToken cancellationToken = default)
    {
        var distinctGameIds = gameIds
            .Select(gameId => gameId.Value)
            .Distinct()
            .ToArray();
        if (distinctGameIds.Length == 0)
            return new Dictionary<GameId, SponsorPlacementSummary>();

        var placements = await _dbContext.GameSponsorPlacements
            .AsNoTracking()
            .Where(placement => distinctGameIds.Contains(placement.GameId))
            .Select(ToPlacementSummary())
            .ToListAsync(cancellationToken);
        return placements.ToDictionary(placement => placement.GameId);
    }

    public async Task ReplaceSponsorPlacementAsync(
        GameId gameId,
        SponsorPlacementInput? placement,
        CancellationToken cancellationToken = default)
    {
        var current = await _dbContext.GameSponsorPlacements
            .SingleOrDefaultAsync(candidate => candidate.GameId == gameId.Value, cancellationToken);
        if (placement is null)
        {
            if (current is null)
                return;

            _dbContext.GameSponsorPlacements.Remove(current);
            await _outboxWriter.SaveAndPublishAsync(
                () => new GameSponsorPlacementChanged(gameId, null, null, null, null, null, null),
                cancellationToken);
            return;
        }

        if (!await _dbContext.Sponsors.AsNoTracking().AnyAsync(
                sponsor => sponsor.Id == placement.SponsorId.Value,
                cancellationToken))
        {
            throw new NotFoundException($"Sponsor with ID {placement.SponsorId.Value} not found");
        }

        if (current is null)
        {
            current = new GameSponsorPlacement { GameId = gameId.Value };
            _dbContext.GameSponsorPlacements.Add(current);
        }

        current.SponsorId = placement.SponsorId.Value;
        current.Context = placement.Context;
        current.Headline = placement.Headline;
        current.SupportLine = placement.SupportLine;
        current.DisplayOrder = placement.DisplayOrder;
        var changedPlacement = current;
        await _outboxWriter.SaveAndPublishAsync(
            () => new GameSponsorPlacementChanged(
                gameId,
                new SponsorPlacementId(changedPlacement.Id),
                new SponsorId(changedPlacement.SponsorId),
                changedPlacement.Context,
                changedPlacement.Headline,
                changedPlacement.SupportLine,
                changedPlacement.DisplayOrder),
            cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<GameSponsorPlacement, SponsorPlacementSummary>> ToPlacementSummary()
    {
        return placement => new SponsorPlacementSummary(
            new SponsorPlacementId(placement.Id),
            new GameId(placement.GameId),
            new SponsorSummary(
                new SponsorId(placement.Sponsor.Id),
                placement.Sponsor.Name,
                placement.Sponsor.SponsorTier,
                placement.Sponsor.LogoUrl,
                placement.Sponsor.InfoUrl,
                placement.Sponsor.Description),
            placement.Context,
            placement.Headline,
            placement.SupportLine,
            placement.DisplayOrder);
    }
}
