using Mercurius.Modules.Shared;
using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Microsoft.EntityFrameworkCore;
using TournamentContracts = Mercurius.Modules.Tournament.Contracts;

namespace Mercurius.Modules.Tournament.Application.Services;

/// <summary>
/// Reads the small public match projection used by player and team profiles.
/// The match query remains set-based and selects one row per tournament before
/// any opponent snapshot data is materialized.
/// </summary>
internal sealed class PublicProfileMatchSummaryReadService
{
    private readonly ITournamentDbContext _dbContext;

    public PublicProfileMatchSummaryReadService(ITournamentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TournamentContracts.PublicProfileMatchSummarySet> GetPublicUserMatchSummariesAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        if (userId.Value == Guid.Empty)
            return new TournamentContracts.PublicProfileMatchSummarySet([], []);

        var activeIndividualRegistrations = _dbContext.TournamentRegistrations
            .AsNoTracking()
            .Where(registration =>
                registration.Kind == TournamentRegistrationKind.Individual &&
                registration.Status == TournamentRegistrationStatus.Active &&
                registration.UserId == userId.Value);

        // The registration snapshot is authoritative for historical team
        // participation. Captains remain eligible when an older registration
        // has no roster rows, which preserves the pre-roster registration shape.
        var activeTeamRegistrations = _dbContext.TournamentRegistrations
            .AsNoTracking()
            .Where(registration =>
                registration.Kind == TournamentRegistrationKind.Team &&
                registration.Status == TournamentRegistrationStatus.Active &&
                registration.TeamId.HasValue &&
                (registration.TeamCaptainUserIdAtRegistration == userId.Value ||
                 registration.RosterMembers.Any(member =>
                     member.UserId == userId.Value &&
                     member.ConfirmationStatus != RosterMemberConfirmationStatus.Pending)));

        var activeTeamIds = await activeTeamRegistrations
            .Select(registration => registration.TeamId!.Value)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var candidateMatches = _dbContext.Matches
            .AsNoTracking()
            .Where(match =>
                match.Id != Guid.Empty &&
                match.TournamentId != Guid.Empty &&
                match.Tournament.Status != TournamentStatus.Canceled &&
                ((match.ParticipationMode == ParticipationMode.Individual &&
                  (match.UserParticipant1Id == userId.Value || match.UserParticipant2Id == userId.Value) &&
                  activeIndividualRegistrations.Any(registration => registration.TournamentId == match.TournamentId)) ||
                 (match.ParticipationMode == ParticipationMode.Team &&
                  ((match.TeamParticipant1Id.HasValue && activeTeamRegistrations.Any(registration =>
                       registration.TournamentId == match.TournamentId &&
                       registration.TeamId == match.TeamParticipant1Id)) ||
                   (match.TeamParticipant2Id.HasValue && activeTeamRegistrations.Any(registration =>
                       registration.TournamentId == match.TournamentId &&
                        registration.TeamId == match.TeamParticipant2Id))))));
        var candidateRows = ProjectCandidateRows(candidateMatches);
        return await GetSummariesAsync(
            candidateRows,
            userId.Value,
            activeTeamIds.ToHashSet(),
            cancellationToken);
    }

    public Task<TournamentContracts.PublicProfileMatchSummarySet> GetPublicTeamMatchSummariesAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default)
    {
        if (teamId.Value == Guid.Empty)
            return Task.FromResult<TournamentContracts.PublicProfileMatchSummarySet>(new([], []));

        var activeTeamRegistrations = _dbContext.TournamentRegistrations
            .AsNoTracking()
            .Where(registration =>
                registration.Kind == TournamentRegistrationKind.Team &&
                registration.Status == TournamentRegistrationStatus.Active &&
                registration.TeamId == teamId.Value);

        var candidateMatches = _dbContext.Matches
            .AsNoTracking()
            .Where(match =>
                match.Id != Guid.Empty &&
                match.TournamentId != Guid.Empty &&
                match.Tournament.Status != TournamentStatus.Canceled &&
                match.ParticipationMode == ParticipationMode.Team &&
                (match.TeamParticipant1Id == teamId.Value || match.TeamParticipant2Id == teamId.Value) &&
                activeTeamRegistrations.Any(registration => registration.TournamentId == match.TournamentId));
        return GetSummariesAsync(
            ProjectCandidateRows(candidateMatches),
            teamId.Value,
            new HashSet<Guid> { teamId.Value },
            cancellationToken);
    }

    private static IQueryable<MatchCandidateRow> ProjectCandidateRows(IQueryable<Match> candidateMatches) =>
        candidateMatches.Select(match => new MatchCandidateRow
        {
            MatchId = match.Id,
            TournamentId = match.TournamentId,
            ParticipationMode = match.ParticipationMode,
            UserParticipant1Id = match.UserParticipant1Id,
            UserParticipant2Id = match.UserParticipant2Id,
            TeamParticipant1Id = match.TeamParticipant1Id,
            TeamParticipant2Id = match.TeamParticipant2Id,
            Participant1IsBYE = match.Participant1IsBYE,
            Participant2IsBYE = match.Participant2IsBYE,
            StartTime = match.StartTime,
            EndTime = match.EndTime,
            EstimatedStartTime = match.EstimatedStartTime,
            EstimatedEndTime = match.EstimatedEndTime,
            LifecycleState = match.LifecycleState,
            ResultKind = match.ResultKind,
            ResultRecordedAtUtc = match.ResultRecordedAtUtc,
            Participant1Score = match.Participant1Score,
            Participant2Score = match.Participant2Score,
            RoundNumber = match.RoundNumber,
            MatchNumber = match.MatchNumber,
            IsLowerBracketMatch = match.IsLowerBracketMatch
        });

    private async Task<TournamentContracts.PublicProfileMatchSummarySet> GetSummariesAsync(
        IQueryable<MatchCandidateRow> candidateRows,
        Guid subjectId,
        IReadOnlySet<Guid> subjectTeamIds,
        CancellationToken cancellationToken)
    {
        var previousEntities = await candidateRows
            .Where(match =>
                (match.LifecycleState == MatchLifecycleState.Completed ||
                 match.LifecycleState == MatchLifecycleState.Forfeited) &&
                (match.LifecycleState == MatchLifecycleState.Forfeited ||
                 (match.Participant1Score.HasValue && match.Participant2Score.HasValue)) &&
                ((match.ParticipationMode == ParticipationMode.Individual &&
                  match.UserParticipant1Id.HasValue &&
                  match.UserParticipant2Id.HasValue &&
                  match.UserParticipant1Id != match.UserParticipant2Id) ||
                 (match.ParticipationMode == ParticipationMode.Team &&
                  match.TeamParticipant1Id.HasValue &&
                  match.TeamParticipant2Id.HasValue &&
                  match.TeamParticipant1Id != match.TeamParticipant2Id)) &&
                !match.Participant1IsBYE &&
                !match.Participant2IsBYE)
            .GroupBy(match => match.TournamentId)
            .Select(group => group
                .OrderByDescending(match =>
                    match.ResultRecordedAtUtc.HasValue && match.ResultRecordedAtUtc.Value != DateTime.MinValue
                        ? match.ResultRecordedAtUtc.Value
                        : match.EndTime != DateTime.MinValue
                            ? match.EndTime
                            : match.StartTime)
                .ThenByDescending(match => match.RoundNumber)
                .ThenByDescending(match => match.MatchNumber)
                .ThenByDescending(match => match.MatchId)
                .First())
            .ToListAsync(cancellationToken);

        // StartTime is an actual lifecycle timestamp. An unstarted match
        // remains eligible even when its estimate is overdue or absent.
        var upcomingEntities = await candidateRows
            .Where(match =>
                match.LifecycleState == MatchLifecycleState.AwaitingEndedConfirmation &&
                match.StartTime == DateTime.MinValue &&
                !match.Participant1IsBYE &&
                !match.Participant2IsBYE &&
                ((match.ParticipationMode == ParticipationMode.Individual &&
                  match.UserParticipant1Id != match.UserParticipant2Id) ||
                 (match.ParticipationMode == ParticipationMode.Team &&
                  match.TeamParticipant1Id != match.TeamParticipant2Id)))
            .GroupBy(match => match.TournamentId)
            .Select(group => group
                .OrderBy(match => match.EstimatedStartTime.HasValue && match.EstimatedStartTime != DateTime.MinValue
                    ? 0
                    : 1)
                .ThenBy(match => match.EstimatedStartTime.HasValue && match.EstimatedStartTime != DateTime.MinValue
                    ? match.EstimatedStartTime.Value
                    : DateTime.MaxValue)
                .ThenBy(match => match.RoundNumber)
                .ThenBy(match => match.MatchNumber)
                .ThenBy(match => match.MatchId)
                .First())
            .ToListAsync(cancellationToken);

        var selectedEntities = previousEntities.Concat(upcomingEntities).ToArray();
        if (selectedEntities.Length == 0)
            return new TournamentContracts.PublicProfileMatchSummarySet([], []);

        var tournamentIds = selectedEntities
            .Select(match => match.TournamentId)
            .Distinct()
            .ToArray();
        var tournamentNames = await _dbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .ToDictionaryAsync(tournament => tournament.Id, tournament => tournament.Name, cancellationToken);

        var previousMatches = previousEntities
            .Select(match => ToCandidateProjection(
                match,
                subjectId,
                subjectTeamIds,
                tournamentNames.GetValueOrDefault(match.TournamentId) ?? string.Empty))
            .OrderBy(candidate => candidate.TournamentName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.TournamentName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.TournamentId)
            .ToArray();
        var upcomingMatches = upcomingEntities
            .Select(match => ToCandidateProjection(
                match,
                subjectId,
                subjectTeamIds,
                tournamentNames.GetValueOrDefault(match.TournamentId) ?? string.Empty))
            .OrderBy(candidate => candidate.TournamentName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.TournamentName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.TournamentId)
            .ToArray();

        var individualParticipantIds = selectedEntities
            .Where(match => match.ParticipationMode == ParticipationMode.Individual)
            .SelectMany(match => new[] { match.UserParticipant1Id, match.UserParticipant2Id })
            .Where(participantId => participantId.HasValue)
            .Select(participantId => participantId!.Value)
            .Distinct()
            .ToArray();
        var teamParticipantIds = selectedEntities
            .Where(match => match.ParticipationMode == ParticipationMode.Team)
            .SelectMany(match => new[] { match.TeamParticipant1Id, match.TeamParticipant2Id })
            .Where(participantId => participantId.HasValue)
            .Select(participantId => participantId!.Value)
            .Distinct()
            .ToArray();
        var registrationSnapshots = await _dbContext.TournamentRegistrations
            .AsNoTracking()
            .Where(registration =>
                tournamentIds.Contains(registration.TournamentId) &&
                ((registration.Kind == TournamentRegistrationKind.Individual &&
                  registration.UserId.HasValue && individualParticipantIds.Contains(registration.UserId.Value)) ||
                 (registration.Kind == TournamentRegistrationKind.Team &&
                  registration.TeamId.HasValue && teamParticipantIds.Contains(registration.TeamId.Value))))
            .OrderBy(registration => registration.TournamentId)
            .ThenBy(registration => registration.Id)
            .Select(registration => new RegistrationSnapshot(
                registration.Id,
                registration.TournamentId,
                registration.Kind,
                registration.UserId,
                registration.UsernameAtRegistration,
                registration.TeamId,
                registration.TeamNameAtRegistration))
            .ToListAsync(cancellationToken);

        var snapshotByParticipant = registrationSnapshots
            .Where(snapshot => snapshot.ParticipantId.HasValue)
            .GroupBy(snapshot => new ParticipantKey(
                snapshot.TournamentId,
                snapshot.Kind,
                snapshot.ParticipantId!.Value))
            .ToDictionary(group => group.Key, group => group.First());

        var previous = previousMatches
            .Select(candidate => ToPublicSummary(candidate, snapshotByParticipant, requireOpponentLabel: true))
            .Where(summary => summary is not null)
            .Select(summary => summary!)
            .ToArray();
        var upcoming = upcomingMatches
            .Select(candidate => ToPublicSummary(candidate, snapshotByParticipant, requireOpponentLabel: false))
            .Where(summary => summary is not null)
            .Select(summary => summary!)
            .ToArray();

        return new TournamentContracts.PublicProfileMatchSummarySet(previous, upcoming);
    }

    private static MatchCandidateProjection ToCandidateProjection(
        MatchCandidateRow match,
        Guid subjectId,
        IReadOnlySet<Guid> subjectTeamIds,
        string tournamentName)
    {
        var participant1Id = NormalizeParticipantId(match.ParticipationMode == ParticipationMode.Individual
            ? match.UserParticipant1Id
            : match.TeamParticipant1Id);
        var participant2Id = NormalizeParticipantId(match.ParticipationMode == ParticipationMode.Individual
            ? match.UserParticipant2Id
            : match.TeamParticipant2Id);
        var subjectIsParticipant1 = match.ParticipationMode == ParticipationMode.Individual
            ? participant1Id == subjectId
            : participant1Id is { } teamId && subjectTeamIds.Contains(teamId);
        var opponentId = subjectIsParticipant1 ? participant2Id : participant1Id;
        var opponentIsSubject = match.ParticipationMode == ParticipationMode.Individual
            ? opponentId == subjectId
            : opponentId is { } opponentTeamId && subjectTeamIds.Contains(opponentTeamId);

        return new MatchCandidateProjection
        {
            MatchId = match.MatchId,
            TournamentId = match.TournamentId,
            TournamentName = tournamentName,
            ParticipationMode = match.ParticipationMode,
            Participant1Id = participant1Id,
            Participant2Id = participant2Id,
            SubjectIsParticipant1 = subjectIsParticipant1,
            OpponentIsSubject = opponentIsSubject,
            Participant1IsBYE = match.Participant1IsBYE,
            Participant2IsBYE = match.Participant2IsBYE,
            StartTime = match.StartTime,
            EndTime = match.EndTime,
            EstimatedStartTime = match.EstimatedStartTime,
            EstimatedEndTime = match.EstimatedEndTime,
            LifecycleState = match.LifecycleState,
            ResultKind = match.ResultKind,
            ResultRecordedAtUtc = match.ResultRecordedAtUtc,
            Participant1Score = match.Participant1Score,
            Participant2Score = match.Participant2Score,
            RoundNumber = match.RoundNumber,
            MatchNumber = match.MatchNumber,
            IsLowerBracketMatch = match.IsLowerBracketMatch
        };
    }

    private static TournamentContracts.PublicProfileMatchSummary? ToPublicSummary(
        MatchCandidateProjection candidate,
        IReadOnlyDictionary<ParticipantKey, RegistrationSnapshot> snapshotByParticipant,
        bool requireOpponentLabel)
    {
        if (candidate.OpponentIsSubject)
            return null;

        var opponentId = candidate.SubjectIsParticipant1
            ? candidate.Participant2Id
            : candidate.Participant1Id;
        var hasOpponent = opponentId.HasValue && opponentId.Value != Guid.Empty;
        var opponentDisplayName = opponentId.HasValue &&
            snapshotByParticipant.TryGetValue(
                new ParticipantKey(candidate.TournamentId, ToRegistrationKind(candidate.ParticipationMode), opponentId.Value),
                out var snapshot)
            ? snapshot.DisplayName
            : null;

        // A previous result needs a public snapshot. For an upcoming match,
        // only an unassigned opposing slot is represented as TBD; an assigned
        // participant with no safe public label is omitted rather than guessed.
        if (string.IsNullOrWhiteSpace(opponentDisplayName) && (requireOpponentLabel || hasOpponent))
            return null;

        var participantScore = candidate.SubjectIsParticipant1
            ? candidate.Participant1Score
            : candidate.Participant2Score;
        var opponentScore = candidate.SubjectIsParticipant1
            ? candidate.Participant2Score
            : candidate.Participant1Score;

        return new TournamentContracts.PublicProfileMatchSummary(
            new MatchId(candidate.MatchId),
            new TournamentId(candidate.TournamentId),
            candidate.TournamentName,
            string.IsNullOrWhiteSpace(opponentDisplayName) ? null : opponentDisplayName,
            !hasOpponent,
            Normalize(candidate.EstimatedStartTime),
            Normalize(candidate.EstimatedEndTime),
            null,
            candidate.LifecycleState is MatchLifecycleState.Completed or MatchLifecycleState.Forfeited
                ? Normalize(candidate.StartTime)
                : null,
            candidate.LifecycleState is MatchLifecycleState.Completed or MatchLifecycleState.Forfeited
                ? Normalize(candidate.ResultRecordedAtUtc) ?? Normalize(candidate.EndTime) ?? Normalize(candidate.StartTime)
                : null,
            (TournamentContracts.MatchLifecycleState)candidate.LifecycleState,
            candidate.ResultKind is { } resultKind ? (TournamentContracts.MatchResultKind)resultKind : null,
            participantScore,
            opponentScore,
            candidate.RoundNumber,
            candidate.MatchNumber,
            candidate.IsLowerBracketMatch);
    }

    private static TournamentRegistrationKind ToRegistrationKind(ParticipationMode participationMode) =>
        participationMode == ParticipationMode.Individual
            ? TournamentRegistrationKind.Individual
            : TournamentRegistrationKind.Team;

    private static DateTime? Normalize(DateTime? value) =>
        value is null || value.Value == DateTime.MinValue
            ? null
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);

    private static DateTime? Normalize(DateTime value) =>
        value == DateTime.MinValue
            ? null
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static Guid? NormalizeParticipantId(Guid? value) =>
        value is null || value.Value == Guid.Empty ? null : value;

    private sealed class MatchCandidateRow
    {
        public Guid MatchId { get; init; }
        public Guid TournamentId { get; init; }
        public ParticipationMode ParticipationMode { get; init; }
        public Guid? UserParticipant1Id { get; init; }
        public Guid? UserParticipant2Id { get; init; }
        public Guid? TeamParticipant1Id { get; init; }
        public Guid? TeamParticipant2Id { get; init; }
        public bool Participant1IsBYE { get; init; }
        public bool Participant2IsBYE { get; init; }
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public DateTime? EstimatedStartTime { get; init; }
        public DateTime? EstimatedEndTime { get; init; }
        public MatchLifecycleState LifecycleState { get; init; }
        public MatchResultKind? ResultKind { get; init; }
        public DateTime? ResultRecordedAtUtc { get; init; }
        public int? Participant1Score { get; init; }
        public int? Participant2Score { get; init; }
        public int RoundNumber { get; init; }
        public int MatchNumber { get; init; }
        public bool IsLowerBracketMatch { get; init; }
    }

    private sealed class MatchCandidateProjection
    {
        public Guid MatchId { get; init; }
        public Guid TournamentId { get; init; }
        public string TournamentName { get; init; } = string.Empty;
        public ParticipationMode ParticipationMode { get; init; }
        public Guid? Participant1Id { get; init; }
        public Guid? Participant2Id { get; init; }
        public bool SubjectIsParticipant1 { get; init; }
        public bool OpponentIsSubject { get; init; }
        public bool Participant1IsBYE { get; init; }
        public bool Participant2IsBYE { get; init; }
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public DateTime? EstimatedStartTime { get; init; }
        public DateTime? EstimatedEndTime { get; init; }
        public MatchLifecycleState LifecycleState { get; init; }
        public MatchResultKind? ResultKind { get; init; }
        public DateTime? ResultRecordedAtUtc { get; init; }
        public int? Participant1Score { get; init; }
        public int? Participant2Score { get; init; }
        public int RoundNumber { get; init; }
        public int MatchNumber { get; init; }
        public bool IsLowerBracketMatch { get; init; }
    }

    private sealed record RegistrationSnapshot(
        Guid Id,
        Guid TournamentId,
        TournamentRegistrationKind Kind,
        Guid? UserId,
        string? UsernameAtRegistration,
        Guid? TeamId,
        string? TeamNameAtRegistration)
    {
        public Guid? ParticipantId => Kind == TournamentRegistrationKind.Individual ? UserId : TeamId;

        public string? DisplayName =>
            Kind == TournamentRegistrationKind.Individual
                ? UsernameAtRegistration
                : TeamNameAtRegistration;
    }

    private readonly record struct ParticipantKey(
        Guid TournamentId,
        TournamentRegistrationKind Kind,
        Guid ParticipantId);
}
