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
        return ToPublicDto(tournament);
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
        tournament.EnsureLeaderboardAttemptsEditable();
        tournament.ValidateLeaderboardAttemptValue(request.Score, request.DurationMilliseconds);
        ValidateSelector(request);

        LeaderboardParticipant participant;
        if (request.ParticipantId.HasValue)
        {
            participant = tournament.FindLeaderboardParticipant(request.ParticipantId.Value);
        }
        else if (request.LinkedUserId.HasValue)
        {
            participant = tournament.FindLeaderboardParticipantByLinkedUserId(request.LinkedUserId.Value)!;
            if (participant is null)
            {
                var profile = await identityModule.GetUserProfileAsync(new UserId(request.LinkedUserId.Value), cancellationToken);
                if (profile is null || profile.IsDeleted)
                    throw new NotFoundException("Linked user not found.");
                participant = tournament.AddLinkedLeaderboardParticipant(
                    request.LinkedUserId.Value,
                    profile.DisplayName ?? profile.Username ?? "Incomplete profile");
                dbContext.LeaderboardParticipants.Add(participant);
            }
        }
        else
        {
            participant = tournament.AddGuestLeaderboardParticipant(request.GuestDisplayName!);
            dbContext.LeaderboardParticipants.Add(participant);
        }

        var attempt = participant.AddAttempt(request.Score, request.DurationMilliseconds, DateTime.UtcNow);
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
        tournament.EnsureLeaderboardAttemptsEditable();
        tournament.ValidateLeaderboardAttemptValue(request.Score, request.DurationMilliseconds);
        var attempt = tournament.CorrectLeaderboardAttempt(
            attemptId,
            request.RowVersion.Value,
            request.Score,
            request.DurationMilliseconds,
            DateTime.UtcNow);
        tournament.LeaderboardRevision++;
        await SaveMutationAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToAttemptDto(attempt);
    }

    public async Task RemoveAttemptAsync(Guid tournamentId, Guid attemptId, Guid rowVersion, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tournament = await GetLeaderboardForMutationAsync(tournamentId, cancellationToken);
        tournament.EnsureLeaderboardAttemptsEditable();
        tournament.RemoveLeaderboardAttempt(attemptId, rowVersion);
        tournament.LeaderboardRevision++;
        await SaveMutationAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    internal static LeaderboardResponseDTO ToPublicDto(TournamentAggregate tournament) => new()
    {
        TournamentId = tournament.Id,
        RankingMetric = (Contracts.LeaderboardRankingMetric)tournament.LeaderboardRankingMetric!.Value,
        Rows = tournament.GetLeaderboardRanking().Select(LeaderboardRowDTO.From).ToList()
    };

    internal static AdminLeaderboardResponseDTO ToAdminDto(TournamentAggregate tournament) => new()
    {
        TournamentId = tournament.Id,
        RankingMetric = (Contracts.LeaderboardRankingMetric)tournament.LeaderboardRankingMetric!.Value,
        Participants = tournament.LeaderboardParticipants.OrderBy(item => item.Id).Select(ToAdminParticipantDto).ToList()
    };

    private static AdminLeaderboardParticipantDTO ToAdminParticipantDto(LeaderboardParticipant participant) => new()
    {
        Id = participant.Id,
        DisplayName = participant.DisplayName,
        ParticipantKind = ToParticipantKind(participant),
        LinkedUserId = participant.LinkedUserId,
        Attempts = participant.Attempts.OrderBy(item => item.CreatedAtUtc).ThenBy(item => item.Id).Select(ToAttemptDto).ToList()
    };

    private static Contracts.LeaderboardParticipantKind ToParticipantKind(LeaderboardParticipant participant) =>
        participant.LinkedUserId.HasValue
            ? Contracts.LeaderboardParticipantKind.LinkedUser
            : Contracts.LeaderboardParticipantKind.Guest;

    private static LeaderboardAttemptDTO ToAttemptDto(LeaderboardAttempt attempt) => new()
    {
        Id = attempt.Id,
        Score = attempt.Score,
        DurationMilliseconds = attempt.DurationMilliseconds,
        CreatedAtUtc = attempt.CreatedAtUtc,
        UpdatedAtUtc = attempt.UpdatedAtUtc,
        RowVersion = attempt.RowVersion
    };

    private IQueryable<TournamentAggregate> GetLeaderboardQuery() => dbContext.Tournaments
        .Where(item => item.BracketType == BracketType.Leaderboard)
        .Include(item => item.LeaderboardParticipants)
            .ThenInclude(participant => participant.Attempts);

    private async Task<TournamentAggregate> GetLeaderboardForMutationAsync(Guid tournamentId, CancellationToken cancellationToken) =>
        await GetLeaderboardQuery().SingleOrDefaultAsync(item => item.Id == tournamentId, cancellationToken)
        ?? throw new NotFoundException("Leaderboard tournament not found.");

    private static void ValidateSelector(RecordLeaderboardAttemptDTO request)
    {
        var count = (request.ParticipantId.HasValue ? 1 : 0)
            + (request.LinkedUserId.HasValue ? 1 : 0)
            + (!string.IsNullOrWhiteSpace(request.GuestDisplayName) ? 1 : 0);
        if (count != 1)
            throw new ValidationException("Exactly one of participantId, linkedUserId, or guestDisplayName is required.");
    }

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
}
