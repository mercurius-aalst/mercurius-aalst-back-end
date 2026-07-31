using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Models;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Sponsorship.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Composition;

internal sealed class LegacySponsorshipModuleAdapter : ISponsorshipModule
{
    private readonly MercuriusDBContext _dbContext;

    public LegacySponsorshipModuleAdapter(MercuriusDBContext dbContext)
    {
        _dbContext = dbContext;
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
            .Select(placement => new SponsorPlacementSummary(
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
                placement.DisplayOrder))
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
            .Select(placement => new SponsorPlacementSummary(
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
                placement.DisplayOrder))
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
            if (current is not null)
                _dbContext.GameSponsorPlacements.Remove(current);
            await _dbContext.SaveChangesAsync(cancellationToken);
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
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
