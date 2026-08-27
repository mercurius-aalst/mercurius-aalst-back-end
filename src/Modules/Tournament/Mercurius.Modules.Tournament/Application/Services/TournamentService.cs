using Mercurius.Modules.Tournament.Application.DTOs.Tournaments;
using Mercurius.Modules.Tournament.Application.DTOs.Placements;
using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Extensions;
using Mercurius.Modules.Tournament.Infrastructure;
using Mercurius.Modules.Media.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Sponsorship.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Eventing;
using TournamentCanceledIntegrationEvent = Mercurius.Modules.Tournament.Contracts.TournamentCanceledIntegrationEvent;
using TournamentCompletedIntegrationEvent = Mercurius.Modules.Tournament.Contracts.TournamentCompletedIntegrationEvent;
using TournamentCreatedIntegrationEvent = Mercurius.Modules.Tournament.Contracts.TournamentCreatedIntegrationEvent;
using TournamentDeletedIntegrationEvent = Mercurius.Modules.Tournament.Contracts.TournamentDeletedIntegrationEvent;
using TournamentResetIntegrationEvent = Mercurius.Modules.Tournament.Contracts.TournamentResetIntegrationEvent;
using TournamentStartedIntegrationEvent = Mercurius.Modules.Tournament.Contracts.TournamentStartedIntegrationEvent;
using TournamentUpdatedIntegrationEvent = Mercurius.Modules.Tournament.Contracts.TournamentUpdatedIntegrationEvent;
using PlacementAssignedIntegrationEvent =
    Mercurius.Modules.Tournament.Contracts.PlacementAssignedIntegrationEvent;

namespace Mercurius.Modules.Tournament.Application.Services;

internal sealed class TournamentService : ITournamentQueries, ITournamentManagementCommands, ITournamentLifecycleCommands
{
    private readonly ITournamentDbContext _dbContext;
    private readonly IMatchModeratorFactory _matchModeratorFactory;
    private readonly IMediaModule _mediaModule;
    private readonly ISponsorshipModule _sponsorshipModule;
    private readonly TournamentDtoMapper _mapper;
    private readonly IModuleEventPublisher _moduleEventPublisher;
    private readonly ILogger<TournamentService> _logger;

    public TournamentService(
        ITournamentDbContext dbContext,
        IMatchModeratorFactory matchModeratorFactory,
        IMediaModule mediaModule,
        ISponsorshipModule sponsorshipModule,
        TournamentDtoMapper mapper,
        IModuleEventPublisher moduleEventPublisher,
        ILogger<TournamentService> logger)
    {
        _dbContext = dbContext;
        _matchModeratorFactory = matchModeratorFactory;
        _mediaModule = mediaModule;
        _sponsorshipModule = sponsorshipModule;
        _mapper = mapper;
        _moduleEventPublisher = moduleEventPublisher;
        _logger = logger;
    }

    public async Task<GetTournamentDTO> CreateTournamentAsync(
        CreateTournamentDTO createTournamentDTO,
        CancellationToken cancellationToken = default)
    {
        if (await TournamentNameExistsAsync(createTournamentDTO.Name, cancellationToken))
            throw new ValidationException($"Tournament {createTournamentDTO.Name} already created");
        if (createTournamentDTO.Image is null)
            throw new ValidationException("A tournament banner/ image is required.");

        var tournament = new TournamentAggregate(
            createTournamentDTO.Name,
            (BracketType)createTournamentDTO.BracketType,
            (GameFormat)createTournamentDTO.Format,
            (GameFormat)createTournamentDTO.FinalsFormat,
            (ParticipationMode)createTournamentDTO.ParticipationMode!.Value,
            createTournamentDTO.TeamSize,
            createTournamentDTO.PlannedStartTime.EnsureUtc(),
            createTournamentDTO.AverageGameDurationMinutes,
            createTournamentDTO.RoundBreakDurationMinutes);

        await using var imageStream = createTournamentDTO.Image.OpenReadStream();
        var asset = await _mediaModule.SaveImageAsync(
            new MediaUpload(
                imageStream,
                createTournamentDTO.Image.FileName,
                createTournamentDTO.Image.ContentType,
                createTournamentDTO.Image.Length),
            cancellationToken);
        var committed = false;
        try
        {
            tournament.ImageUrl = asset.Url;
            _dbContext.Tournaments.Add(tournament);
            _moduleEventPublisher.Publish(new TournamentCreatedIntegrationEvent(new TournamentId(tournament.Id), tournament.Name));
            await _dbContext.SaveChangesAsync(cancellationToken);
            committed = true;
        }
        catch
        {
            if (!committed)
                await DeleteImageBestEffortAsync(asset.Url, "compensate an uncommitted tournament image");
            throw;
        }

        return await GetTournamentByIdAsync(tournament.Id, cancellationToken);
    }

    public async Task<GetTournamentDTO> GetTournamentByIdAsync(
        Guid tournamentId,
        CancellationToken cancellationToken = default)
    {
        var tournament = await CreateTournamentDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(tournament => tournament.Id == tournamentId, cancellationToken);
        if (tournament is null)
            throw new NotFoundException("Tournament not found");

        return await _mapper.ToGetTournamentDtoAsync(tournament, cancellationToken);
    }

    public async Task<IReadOnlyList<GetTournamentDTO>> GetAllTournamentsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return [];
        }

        var tournaments = await CreateTournamentListQuery()
            .AsNoTracking()
            .OrderBy(tournament => tournament.PlannedStartTime)
            .ThenBy(tournament => tournament.Name)
            .ThenBy(tournament => tournament.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return await _mapper.ToGetTournamentDtosAsync(tournaments, cancellationToken);
    }

    public async Task<GetTournamentDTO> UpdateTournamentAsync(
        Guid id,
        UpdateTournamentDTO tournamentDTO,
        CancellationToken cancellationToken = default)
    {
        var tournament = await GetTournamentForMutationAsync(id, cancellationToken);
        if (tournament.Name != tournamentDTO.Name && await TournamentNameExistsAsync(tournamentDTO.Name, cancellationToken))
            throw new ValidationException($"Tournament {tournamentDTO.Name} already exists");

        tournament.Update(
            tournamentDTO.Name,
            (BracketType)tournamentDTO.BracketType,
            (GameFormat)tournamentDTO.Format,
            (GameFormat)tournamentDTO.FinalsFormat,
            (ParticipationMode)tournamentDTO.ParticipationMode!.Value,
            tournamentDTO.TeamSize,
            tournamentDTO.PlannedStartTime.EnsureUtc(),
            tournamentDTO.AverageGameDurationMinutes,
            tournamentDTO.RoundBreakDurationMinutes);

        var previousImageUrl = tournament.ImageUrl;
        string? newImageUrl = null;
        if (tournamentDTO.Image is not null)
        {
            await using var imageStream = tournamentDTO.Image.OpenReadStream();
            var asset = await _mediaModule.SaveImageAsync(
                new MediaUpload(
                    imageStream,
                    tournamentDTO.Image.FileName,
                    tournamentDTO.Image.ContentType,
                    tournamentDTO.Image.Length),
                cancellationToken);
            newImageUrl = asset.Url;
        }

        var committed = false;
        try
        {
            if (newImageUrl is not null)
                tournament.ImageUrl = newImageUrl;

            _moduleEventPublisher.Publish(new TournamentUpdatedIntegrationEvent(new TournamentId(tournament.Id), tournament.Name));
            await _dbContext.SaveChangesAsync(cancellationToken);
            committed = true;
        }
        catch
        {
            if (!committed && !string.Equals(newImageUrl, previousImageUrl, StringComparison.Ordinal))
                await DeleteImageBestEffortAsync(newImageUrl, "compensate an uncommitted tournament image replacement");
            throw;
        }

        if (newImageUrl is not null && !string.Equals(previousImageUrl, newImageUrl, StringComparison.Ordinal))
            await DeleteImageBestEffortAsync(previousImageUrl, "retire a replaced tournament image");

        return await GetTournamentByIdAsync(tournament.Id, cancellationToken);
    }

    public async Task DeleteTournamentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tournament = await GetTournamentForSimpleMutationAsync(id, cancellationToken);
        if (tournament.Status == TournamentStatus.InProgress)
            throw new ValidationException("Tournament cannot be deleted when already in progress.");

        var imageUrl = tournament.ImageUrl;
        _dbContext.Tournaments.Remove(tournament);
        _moduleEventPublisher.Publish(new TournamentDeletedIntegrationEvent(new TournamentId(tournament.Id)));
        await _dbContext.SaveChangesAsync(cancellationToken);
        await DeleteImageBestEffortAsync(imageUrl, "retire a deleted tournament image");
    }

    public async Task CancelTournamentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tournament = await GetTournamentForSimpleMutationAsync(id, cancellationToken);
        tournament.Cancel();
        _moduleEventPublisher.Publish(new TournamentCanceledIntegrationEvent(new TournamentId(tournament.Id), tournament.Name));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task StartTournamentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tournament = await GetTournamentForMutationAsync(id, cancellationToken);
        tournament.Start();
        var matchModerator = _matchModeratorFactory.GetMatchModerator(tournament.BracketType);
        tournament.Matches = matchModerator.GenerateMatchesForTournament(tournament).ToList();
        AssignEstimatedSchedule(tournament);
        _moduleEventPublisher.Publish(new TournamentStartedIntegrationEvent(new TournamentId(tournament.Id), tournament.StartTime));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<GetPlacementDTO>> CompleteTournamentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tournament = await GetTournamentForMutationAsync(id, cancellationToken);
        tournament.Complete();
        var matchModerator = _matchModeratorFactory.GetMatchModerator(tournament.BracketType);
        matchModerator.DeterminePlacements(tournament);
        _moduleEventPublisher.Publish(new TournamentCompletedIntegrationEvent(new TournamentId(tournament.Id), tournament.EndTime));
        foreach (var placement in tournament.Placements)
        {
            foreach (var participantId in placement.Users.Select(user => user.UserId)
                         .Concat(placement.Teams.Select(team => team.TeamId)))
            {
                _moduleEventPublisher.Publish(new PlacementAssignedIntegrationEvent(
                    new TournamentId(tournament.Id),
                    placement.Place,
                    participantId));
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        var mapped = await _mapper.ToGetTournamentDtoAsync(tournament, cancellationToken);
        return mapped.Placements;
    }

    public async Task ResetTournamentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tournament = await GetTournamentForMutationAsync(id, cancellationToken);
        tournament.Reset();
        _moduleEventPublisher.Publish(new TournamentResetIntegrationEvent(new TournamentId(tournament.Id)));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GetTournamentDTO> ReplaceSponsorPlacementsAsync(
        Guid id,
        ReplaceTournamentSponsorsDTO sponsorDTO,
        CancellationToken cancellationToken = default)
    {
        _ = await GetTournamentForSimpleMutationAsync(id, cancellationToken);
        var placements = sponsorDTO.SponsorPlacements ?? [];
        if (placements.Count > 1)
            throw new ValidationException("A tournament can only have one sponsor.");

        var placement = placements.SingleOrDefault();
        await _sponsorshipModule.ReplaceSponsorPlacementAsync(
            new TournamentId(id),
            placement is null
                ? null
                : new SponsorPlacementInput(
                    new SponsorId(placement.SponsorId),
                    placement.Context,
                    placement.Headline,
                    placement.SupportLine,
                    placement.DisplayOrder),
            cancellationToken);

        return await GetTournamentByIdAsync(id, cancellationToken);
    }

    private Task<bool> TournamentNameExistsAsync(string name, CancellationToken cancellationToken) =>
        _dbContext.Tournaments.AnyAsync(tournament => tournament.Name == name, cancellationToken);

    private async Task<TournamentAggregate> GetTournamentForMutationAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        var tournament = await CreateTournamentDetailsQuery()
            .FirstOrDefaultAsync(tournament => tournament.Id == tournamentId, cancellationToken);
        if (tournament is null)
            throw new NotFoundException("Tournament not found");
        return tournament;
    }

    private async Task<TournamentAggregate> GetTournamentForSimpleMutationAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        var tournament = await _dbContext.Tournaments.FirstOrDefaultAsync(tournament => tournament.Id == tournamentId, cancellationToken);
        if (tournament is null)
            throw new NotFoundException("Tournament not found");
        return tournament;
    }

    private IQueryable<TournamentAggregate> CreateTournamentDetailsQuery()
    {
        return _dbContext.Tournaments
            .AsSplitQuery()
            .Include(tournament => tournament.TournamentRegistrations)
                .ThenInclude(registration => registration.RosterMembers)
            .Include(tournament => tournament.Matches)
            .Include(tournament => tournament.Placements)
                .ThenInclude(placement => placement.Users)
            .Include(tournament => tournament.Placements)
                .ThenInclude(placement => placement.Teams);
    }

    private IQueryable<TournamentAggregate> CreateTournamentListQuery()
    {
        return _dbContext.Tournaments;
    }

    private static void AssignEstimatedSchedule(TournamentAggregate tournament)
    {
        if (tournament.Matches.Count == 0)
        {
            tournament.EstimatedEndTime = null;
            return;
        }

        var currentRoundStart = tournament.PlannedStartTime;
        DateTime? latestEnd = null;
        foreach (var round in tournament.Matches
                     .GroupBy(match => match.RoundNumber)
                     .OrderBy(group => group.Key))
        {
            var roundDuration = TimeSpan.Zero;
            foreach (var match in round.OrderBy(match => match.MatchNumber))
            {
                var matchDuration = TimeSpan.FromMinutes(
                    tournament.AverageGameDurationMinutes * GetDurationMultiplier(match.Format));
                var estimatedEnd = AddScheduleTime(currentRoundStart, matchDuration);
                match.SetEstimatedWindow(currentRoundStart, estimatedEnd);
                if (matchDuration > roundDuration)
                    roundDuration = matchDuration;
                if (!latestEnd.HasValue || estimatedEnd > latestEnd.Value)
                    latestEnd = estimatedEnd;
            }

            currentRoundStart = AddScheduleTime(
                AddScheduleTime(currentRoundStart, roundDuration),
                TimeSpan.FromMinutes(tournament.RoundBreakDurationMinutes));
        }

        tournament.EstimatedEndTime = latestEnd;
    }

    private static int GetDurationMultiplier(GameFormat format)
    {
        return format switch
        {
            GameFormat.BestOf1 => 1,
            GameFormat.BestOf3 => 3,
            GameFormat.BestOf5 => 5,
            _ => 1
        };
    }

    private static DateTime AddScheduleTime(DateTime timestamp, TimeSpan duration)
    {
        try
        {
            return timestamp.Add(duration);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ValidationException("Estimated tournament schedule exceeds supported date range.");
        }
    }

    private async Task DeleteImageBestEffortAsync(string? imageUrl, string action)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        try
        {
            await _mediaModule.DeleteImageAsync(imageUrl, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to {MediaCleanupAction} at {MediaUrl}.", action, imageUrl);
        }
    }
}
