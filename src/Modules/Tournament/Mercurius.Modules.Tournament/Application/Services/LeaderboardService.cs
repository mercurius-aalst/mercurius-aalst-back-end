using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Tournament.Application.DTOs.Leaderboards;
using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Contracts = Mercurius.Modules.Tournament.Contracts;

namespace Mercurius.Modules.Tournament.Application.Services;

internal sealed class LeaderboardService(ITournamentDbContext dbContext, IIdentityModule identityModule) : ILeaderboardService
{
    public async Task<LeaderboardResponseDTO> GetPublicLeaderboardAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await GetLeaderboardQuery().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == tournamentId, cancellationToken)
            ?? throw new NotFoundException("Leaderboard tournament not found.");
        return LeaderboardRanking.ToPublicDto(tournament);
    }

    public async Task<AdminLeaderboardResponseDTO> GetAdminLeaderboardAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await GetLeaderboardQuery().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == tournamentId, cancellationToken)
            ?? throw new NotFoundException("Leaderboard tournament not found.");
        return ToAdminDto(tournament);
    }

    public async Task<AdminLeaderboardParticipantDTO> RecordAttemptAsync(
        Guid tournamentId,
        RecordLeaderboardAttemptDTO request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tournament = await GetLeaderboardForMutationAsync(tournamentId, cancellationToken);
        EnsureInProgress(tournament);
        ValidateValue(tournament, request.Score, request.DurationMilliseconds);
        ValidateSelector(request);

        LeaderboardParticipant participant;
        if (request.ParticipantId.HasValue)
        {
            participant = tournament.LeaderboardParticipants.SingleOrDefault(item => item.Id == request.ParticipantId.Value)
                ?? throw new NotFoundException("Leaderboard participant not found.");
        }
        else if (request.LinkedUserId.HasValue)
        {
            participant = tournament.LeaderboardParticipants.SingleOrDefault(item => item.LinkedUserId == request.LinkedUserId.Value)!;
            if (participant is null)
            {
                var profile = await identityModule.GetUserProfileAsync(new UserId(request.LinkedUserId.Value), cancellationToken);
                if (profile is null || profile.IsDeleted)
                    throw new NotFoundException("Linked user not found.");
                participant = new LeaderboardParticipant
                {
                    Id = Guid.NewGuid(),
                    TournamentId = tournament.Id,
                    LinkedUserId = request.LinkedUserId,
                    DisplayName = profile.DisplayName ?? profile.Username ?? "Incomplete profile"
                };
                tournament.LeaderboardParticipants.Add(participant);
                dbContext.LeaderboardParticipants.Add(participant);
            }
        }
        else
        {
            participant = new LeaderboardParticipant
            {
                Id = Guid.NewGuid(),
                TournamentId = tournament.Id,
                DisplayName = request.GuestDisplayName!.Trim()
            };
            tournament.LeaderboardParticipants.Add(participant);
            dbContext.LeaderboardParticipants.Add(participant);
        }

        var now = DateTime.UtcNow;
        var attempt = new LeaderboardAttempt
        {
            Id = Guid.NewGuid(),
            Score = request.Score,
            DurationMilliseconds = request.DurationMilliseconds,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            RowVersion = Guid.NewGuid()
        };
        participant.Attempts.Add(attempt);
        dbContext.LeaderboardAttempts.Add(attempt);
        tournament.LeaderboardRevision++;
        await SaveMutationAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToAdminParticipantDto(participant);
    }

    public async Task<LeaderboardAttemptDTO> UpdateAttemptAsync(
        Guid tournamentId,
        Guid attemptId,
        UpdateLeaderboardAttemptDTO request,
        CancellationToken cancellationToken = default)
    {
        if (!request.RowVersion.HasValue)
            throw new ValidationException("rowVersion is required.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tournament = await GetLeaderboardForMutationAsync(tournamentId, cancellationToken);
        EnsureInProgress(tournament);
        ValidateValue(tournament, request.Score, request.DurationMilliseconds);
        var attempt = tournament.LeaderboardParticipants.SelectMany(item => item.Attempts)
            .SingleOrDefault(item => item.Id == attemptId)
            ?? throw new NotFoundException("Leaderboard attempt not found.");
        if (attempt.RowVersion != request.RowVersion.Value)
            throw new ConflictException("leaderboard_attempt_changed", "The leaderboard attempt changed. Refresh and try again.");
        attempt.Score = request.Score;
        attempt.DurationMilliseconds = request.DurationMilliseconds;
        attempt.UpdatedAtUtc = DateTime.UtcNow;
        attempt.RowVersion = Guid.NewGuid();
        tournament.LeaderboardRevision++;
        await SaveMutationAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToAttemptDto(attempt);
    }

    public async Task RemoveAttemptAsync(Guid tournamentId, Guid attemptId, Guid rowVersion, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tournament = await GetLeaderboardForMutationAsync(tournamentId, cancellationToken);
        EnsureInProgress(tournament);
        var attempt = tournament.LeaderboardParticipants.SelectMany(item => item.Attempts)
            .SingleOrDefault(item => item.Id == attemptId)
            ?? throw new NotFoundException("Leaderboard attempt not found.");
        if (attempt.RowVersion != rowVersion)
            throw new ConflictException("leaderboard_attempt_changed", "The leaderboard attempt changed. Refresh and try again.");
        dbContext.LeaderboardAttempts.Remove(attempt);
        tournament.LeaderboardRevision++;
        await SaveMutationAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private IQueryable<TournamentAggregate> GetLeaderboardQuery() => dbContext.Tournaments
        .Where(item => item.BracketType == BracketType.Leaderboard)
        .Include(item => item.LeaderboardParticipants)
            .ThenInclude(participant => participant.Attempts);

    private async Task<TournamentAggregate> GetLeaderboardForMutationAsync(Guid tournamentId, CancellationToken cancellationToken) =>
        await GetLeaderboardQuery().SingleOrDefaultAsync(item => item.Id == tournamentId, cancellationToken)
        ?? throw new NotFoundException("Leaderboard tournament not found.");

    private static void EnsureInProgress(TournamentAggregate tournament)
    {
        if (tournament.Status != TournamentStatus.InProgress)
            throw new ValidationException("Leaderboard attempts can only be changed while the tournament is in progress.");
    }

    private static void ValidateSelector(RecordLeaderboardAttemptDTO request)
    {
        var count = (request.ParticipantId.HasValue ? 1 : 0)
            + (request.LinkedUserId.HasValue ? 1 : 0)
            + (!string.IsNullOrWhiteSpace(request.GuestDisplayName) ? 1 : 0);
        if (count != 1)
            throw new ValidationException("Exactly one of participantId, linkedUserId, or guestDisplayName is required.");
    }

    internal static void ValidateValue(TournamentAggregate tournament, decimal? score, long? durationMilliseconds)
    {
        if (tournament.LeaderboardRankingMetric == LeaderboardRankingMetric.HighestScore)
        {
            if (!score.HasValue || durationMilliseconds.HasValue || score.Value < 0 || score.Value >= 1_000_000_000_000m || GetScale(score.Value) > 6)
                throw new ValidationException("Highest-score attempts require a non-negative score with at most 12 integral and 6 fractional digits.");
            return;
        }
        if (tournament.LeaderboardRankingMetric == LeaderboardRankingMetric.FastestTime)
        {
            if (!durationMilliseconds.HasValue || score.HasValue || durationMilliseconds.Value <= 0)
                throw new ValidationException("Fastest-time attempts require a positive durationMilliseconds value.");
            return;
        }
        throw new ValidationException("Tournament has no supported leaderboard ranking metric.");
    }

    private static int GetScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;

    private async Task SaveMutationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("leaderboard_changed", "The leaderboard changed. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (IsLinkedUserUniqueViolation(exception))
        {
            throw new ConflictException("leaderboard_participant_exists", "The linked user already participates in this leaderboard.");
        }
    }

    private static bool IsLinkedUserUniqueViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current.Message.Contains("IX_leaderboard_participants_TournamentId_LinkedUserId", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static AdminLeaderboardResponseDTO ToAdminDto(TournamentAggregate tournament) => new()
    {
        TournamentId = tournament.Id,
        RankingMetric = (Contracts.LeaderboardRankingMetric)tournament.LeaderboardRankingMetric!.Value,
        Participants = tournament.LeaderboardParticipants.OrderBy(item => item.Id).Select(ToAdminParticipantDto).ToList()
    };

    private static AdminLeaderboardParticipantDTO ToAdminParticipantDto(LeaderboardParticipant participant) => new()
    {
        Id = participant.Id,
        DisplayName = participant.DisplayName,
        ParticipantKind = participant.LinkedUserId.HasValue ? Contracts.LeaderboardParticipantKind.LinkedUser : Contracts.LeaderboardParticipantKind.Guest,
        LinkedUserId = participant.LinkedUserId,
        Attempts = participant.Attempts.OrderBy(item => item.CreatedAtUtc).ThenBy(item => item.Id).Select(ToAttemptDto).ToList()
    };

    private static LeaderboardAttemptDTO ToAttemptDto(LeaderboardAttempt attempt) => new()
    {
        Id = attempt.Id,
        Score = attempt.Score,
        DurationMilliseconds = attempt.DurationMilliseconds,
        CreatedAtUtc = attempt.CreatedAtUtc,
        UpdatedAtUtc = attempt.UpdatedAtUtc,
        RowVersion = attempt.RowVersion
    };
}

internal static class LeaderboardRanking
{
    internal static IReadOnlyList<LeaderboardRowDTO> Build(TournamentAggregate tournament)
    {
        if (!tournament.LeaderboardRankingMetric.HasValue)
            return [];
        var candidates = tournament.LeaderboardParticipants
            .Select(participant => new
            {
                Participant = participant,
                Score = participant.Attempts.Where(item => item.Score.HasValue).Select(item => item.Score).Max(),
                Duration = participant.Attempts.Where(item => item.DurationMilliseconds.HasValue).Select(item => item.DurationMilliseconds).Min()
            })
            .Where(item => tournament.LeaderboardRankingMetric == LeaderboardRankingMetric.HighestScore ? item.Score.HasValue : item.Duration.HasValue);

        var ordered = tournament.LeaderboardRankingMetric == LeaderboardRankingMetric.HighestScore
            ? candidates.OrderByDescending(item => item.Score).ThenBy(item => item.Participant.Id).ToList()
            : candidates.OrderBy(item => item.Duration).ThenBy(item => item.Participant.Id).ToList();
        var rows = new List<LeaderboardRowDTO>(ordered.Count);
        decimal? previousScore = null;
        long? previousDuration = null;
        for (var index = 0; index < ordered.Count; index++)
        {
            var item = ordered[index];
            var tied = index > 0 && (tournament.LeaderboardRankingMetric == LeaderboardRankingMetric.HighestScore
                ? item.Score == previousScore
                : item.Duration == previousDuration);
            rows.Add(new LeaderboardRowDTO
            {
                Rank = tied ? rows[^1].Rank : index + 1,
                ParticipantId = item.Participant.Id,
                DisplayName = item.Participant.DisplayName,
                ParticipantKind = item.Participant.LinkedUserId.HasValue ? Contracts.LeaderboardParticipantKind.LinkedUser : Contracts.LeaderboardParticipantKind.Guest,
                LinkedUserId = item.Participant.LinkedUserId,
                Score = item.Score,
                DurationMilliseconds = item.Duration
            });
            previousScore = item.Score;
            previousDuration = item.Duration;
        }
        return rows;
    }

    internal static LeaderboardResponseDTO ToPublicDto(TournamentAggregate tournament) => new()
    {
        TournamentId = tournament.Id,
        RankingMetric = (Contracts.LeaderboardRankingMetric)tournament.LeaderboardRankingMetric!.Value,
        Rows = Build(tournament)
    };
}
