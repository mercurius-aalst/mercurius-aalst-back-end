using Mercurius.Modules.Tournament.Application.DTOs.Tournaments;
using Mercurius.Modules.Tournament.Application.DTOs.Matches;
using Mercurius.Modules.Tournament.Application.DTOs.Participants;
using Mercurius.Modules.Tournament.Application.DTOs.Placements;
using Mercurius.Modules.Tournament.Application.DTOs.Registrations;
using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.Modules.Teams.Contracts;

namespace Mercurius.Modules.Tournament.Application;

internal sealed class TournamentDtoMapper
{
    private readonly RegistrationMappingContextBuilder _contextBuilder;
    private readonly ISponsorshipModule _sponsorshipModule;

    public TournamentDtoMapper(
        RegistrationMappingContextBuilder contextBuilder,
        ISponsorshipModule sponsorshipModule)
    {
        _contextBuilder = contextBuilder;
        _sponsorshipModule = sponsorshipModule;
    }

    public async Task<GetTournamentDTO> ToGetTournamentDtoAsync(TournamentAggregate tournament, CancellationToken cancellationToken)
    {
        var sponsorPlacement = await _sponsorshipModule.GetSponsorPlacementAsync(
            new TournamentId(tournament.Id),
            cancellationToken);
        var context = await _contextBuilder.BuildAsync(
            tournament.TournamentRegistrations.ToList(),
            tournament.Placements.ToList(),
            cancellationToken);

        return ToGetTournamentDto(tournament, context, sponsorPlacement);
    }

    public async Task<IReadOnlyList<GetTournamentDTO>> ToGetTournamentDtosAsync(
        IReadOnlyCollection<TournamentAggregate> tournaments,
        CancellationToken cancellationToken)
    {
        if (tournaments.Count == 0)
            return [];

        var registrations = tournaments
            .SelectMany(tournament => tournament.TournamentRegistrations)
            .ToList();
        var placements = tournaments
            .SelectMany(tournament => tournament.Placements)
            .ToList();
        var tournamentIds = tournaments
            .Select(tournament => new TournamentId(tournament.Id))
            .ToArray();

        var context = registrations.Count == 0 && placements.Count == 0
            ? new RegistrationMappingContext(
                new Dictionary<UserId, UserProfileSummary>(),
                new Dictionary<TeamId, TeamRosterSnapshot>())
            : await _contextBuilder.BuildAsync(registrations, placements, cancellationToken);
        var sponsorPlacements = await _sponsorshipModule.GetSponsorPlacementsAsync(tournamentIds, cancellationToken);

        return tournaments
            .Select(tournament => ToGetTournamentDto(
                tournament,
                context,
                sponsorPlacements.GetValueOrDefault(new TournamentId(tournament.Id))))
            .ToList();
    }

    private GetTournamentDTO ToGetTournamentDto(
        TournamentAggregate tournament,
        RegistrationMappingContext context,
        SponsorPlacementSummary? sponsorPlacement)
    {

        return new GetTournamentDTO
        {
            Id = tournament.Id,
            Name = tournament.Name,
            StartTime = tournament.StartTime,
            EndTime = tournament.EndTime,
            PlannedStartTime = tournament.PlannedStartTime,
            AverageGameDurationMinutes = tournament.AverageGameDurationMinutes,
            RoundBreakDurationMinutes = tournament.RoundBreakDurationMinutes,
            EstimatedEndTime = tournament.EstimatedEndTime,
            Status = (Contracts.TournamentStatus)tournament.Status,
            BracketType = (Contracts.BracketType)tournament.BracketType,
            Format = (Contracts.GameFormat)tournament.Format,
            FinalsFormat = (Contracts.GameFormat)tournament.FinalsFormat,
            ParticipationMode = (Contracts.ParticipationMode)tournament.ParticipationMode,
            TeamSize = tournament.TeamSize,
            ImageUrl = tournament.ImageUrl,
            Placements = tournament.Placements
                .OrderBy(placement => placement.Place)
                .Select(placement => ToGetPlacementDto(placement, context))
                .ToList(),
            SponsorPlacement = sponsorPlacement is null ? null : ToSponsorPlacementDto(sponsorPlacement),
            Matches = tournament.Matches
                .OrderBy(match => match.RoundNumber)
                .ThenBy(match => match.MatchNumber)
                .Select(match => ToGetMatchDto(match))
                .ToList(),
            Registrations = tournament.TournamentRegistrations
                .Where(registration => registration.Status == TournamentRegistrationStatus.Active)
                .OrderBy(registration => registration.Kind)
                .ThenBy(registration => registration.CreatedAtUtc)
                .Select(registration => ToPublicRegistrationDto(registration, context))
                .ToList()
        };
    }

    public async Task<TournamentRegistrationDTO> ToRegistrationDtoAsync(
        TournamentRegistration registration,
        CancellationToken cancellationToken)
    {
        var context = await _contextBuilder.BuildAsync([registration], [], cancellationToken);
        return ToRegistrationDto(registration, context);
    }

    public async Task<IReadOnlyList<AdminTournamentRegistrationDTO>> ToAdminRegistrationDtosAsync(
        IReadOnlyCollection<TournamentRegistration> registrations,
        CancellationToken cancellationToken)
    {
        var context = await _contextBuilder.BuildAsync(registrations, [], cancellationToken);
        return registrations
            .Select(registration =>
            {
                var source = ToRegistrationDto(registration, context);
                return new AdminTournamentRegistrationDTO
                {
                    Id = source.Id,
                    TournamentId = source.TournamentId,
                    Kind = source.Kind,
                    Status = source.Status,
                    User = source.User,
                    Team = source.Team,
                    RosterMembers = source.RosterMembers,
                    CreatedAtUtc = source.CreatedAtUtc,
                    UpdatedAtUtc = source.UpdatedAtUtc
                };
            })
            .ToList();
    }

    public TournamentRegistrationDTO ToRegistrationDto(
        TournamentRegistration registration,
        RegistrationMappingContext context)
    {
        return new TournamentRegistrationDTO
        {
            Id = registration.Id,
            TournamentId = registration.TournamentId,
            Kind = (Contracts.TournamentRegistrationKind)registration.Kind,
            Status = (Contracts.TournamentRegistrationStatus)registration.Status,
            User = registration.UserId.HasValue
                ? GetPublicUser(
                    registration.UserId.Value,
                    registration.UsernameAtRegistration,
                    registration.UsernameAtRegistration,
                    context)
                : null,
            Team = registration.TeamId.HasValue
                ? GetTeamParticipant(registration, context)
                : null,
            RosterMembers = registration.RosterMembers
                .OrderByDescending(member => member.IsCaptain)
                .ThenBy(member => member.UsernameAtRegistration, StringComparer.OrdinalIgnoreCase)
                .Select(member => ToRosterMemberDto(member, context))
                .ToList(),
            CreatedAtUtc = registration.CreatedAtUtc,
            UpdatedAtUtc = registration.UpdatedAtUtc
        };
    }

    public TournamentRosterMemberDTO ToRosterMemberDto(
        TournamentRegistrationRosterMember member,
        RegistrationMappingContext context)
    {
        return new TournamentRosterMemberDTO
        {
            Id = member.Id,
            User = GetPublicUser(
                member.UserId,
                member.UsernameAtRegistration,
                member.DisplayNameAtRegistration,
                context),
            IsCaptain = member.IsCaptain,
            ConfirmationStatus = (Contracts.RosterMemberConfirmationStatus)member.ConfirmationStatus
        };
    }

    internal PublicTournamentRegistrationDTO ToPublicRegistrationDto(
        TournamentRegistration registration,
        RegistrationMappingContext context)
    {
        return new PublicTournamentRegistrationDTO
        {
            Id = registration.Id,
            TournamentId = registration.TournamentId,
            Kind = (Contracts.TournamentRegistrationKind)registration.Kind,
            Status = (Contracts.TournamentRegistrationStatus)registration.Status,
            User = registration.UserId.HasValue
                ? GetPublicUser(
                    registration.UserId.Value,
                    registration.UsernameAtRegistration,
                    registration.UsernameAtRegistration,
                    context)
                : null,
            Team = registration.TeamId.HasValue
                ? new PublicTournamentTeamDTO
                {
                    Id = registration.TeamId.Value,
                    Name = registration.TeamNameAtRegistration ?? string.Empty,
                    CaptainUserId = registration.TeamCaptainUserIdAtRegistration ?? Guid.Empty,
                    LogoUrl = registration.TeamLogoUrlAtRegistration
                }
                : null,
            RosterMembers = registration.RosterMembers
                .OrderByDescending(member => member.IsCaptain)
                .ThenBy(member => member.UsernameAtRegistration, StringComparer.OrdinalIgnoreCase)
                .Select(member => new PublicTournamentRosterMemberDTO
                {
                    User = GetPublicUser(
                        member.UserId,
                        member.UsernameAtRegistration,
                        member.DisplayNameAtRegistration,
                        context),
                    IsCaptain = member.IsCaptain
                })
                .ToList()
        };
    }

    internal static GetMatchDTO ToGetMatchDto(Match match, bool canViewPrivateReports = false)
    {
        return new GetMatchDTO
        {
            Id = match.Id,
            StartTime = match.StartTime,
            EndTime = match.EndTime,
            EstimatedStartTime = match.EstimatedStartTime,
            EstimatedEndTime = match.EstimatedEndTime,
            BracketType = (Contracts.BracketType)match.BracketType,
            Format = (Contracts.GameFormat)match.Format,
            ParticipationMode = (Contracts.ParticipationMode)match.ParticipationMode,
            RoundNumber = match.RoundNumber,
            MatchNumber = match.MatchNumber,
            IsLowerBracketMatch = match.IsLowerBracketMatch,
            TournamentId = match.TournamentId,
            UserParticipant1Id = match.UserParticipant1Id,
            UserParticipant2Id = match.UserParticipant2Id,
            TeamParticipant1Id = match.TeamParticipant1Id,
            TeamParticipant2Id = match.TeamParticipant2Id,
            Participant1IsBYE = match.Participant1IsBYE,
            Participant2IsBYE = match.Participant2IsBYE,
            UserWinnerId = match.UserWinnerId,
            UserLoserId = match.UserLoserId,
            TeamWinnerId = match.TeamWinnerId,
            TeamLoserId = match.TeamLoserId,
            Participant1Score = match.Participant1Score,
            Participant2Score = match.Participant2Score,
            WinnerNextMatchId = match.WinnerNextMatchId,
            LoserNextMatchId = match.LoserNextMatchId,
            LifecycleState = (Contracts.MatchLifecycleState)match.LifecycleState,
            Participant1Ended = match.Participant1Ended,
            Participant2Ended = match.Participant2Ended,
            Participant1ReportedScore1 = canViewPrivateReports ? match.Participant1ReportedScore1 : null,
            Participant1ReportedScore2 = canViewPrivateReports ? match.Participant1ReportedScore2 : null,
            Participant2ReportedScore1 = canViewPrivateReports ? match.Participant2ReportedScore1 : null,
            Participant2ReportedScore2 = canViewPrivateReports ? match.Participant2ReportedScore2 : null,
            ScoreConfirmationDeadlineUtc = match.ScoreConfirmationDeadlineUtc,
            CorrectionDeadlineUtc = match.CorrectionDeadlineUtc,
            Participant1CorrectionCount = match.Participant1CorrectionCount,
            Participant2CorrectionCount = match.Participant2CorrectionCount,
            ForfeitedParticipantNumber = match.ForfeitedParticipantNumber,
            ResultKind = match.ResultKind.HasValue ? (Contracts.MatchResultKind)match.ResultKind.Value : null,
            ResultVersion = match.ResultVersion
        };
    }

    internal static GetMatchActionStateDTO ToGetMatchActionStateDto(
        Match match,
        Contracts.MatchParticipantSide? authorizedParticipant,
        bool tournamentInProgress = true,
        bool canViewPrivateReports = false,
        bool canResolve = false,
        string? resolveBlockedReason = null,
        bool canForceForfeit = false,
        string? forceForfeitBlockedReason = null,
        bool canReverse = false,
        string? reverseBlockedReason = null)
    {
        return new GetMatchActionStateDTO
        {
            Match = ToGetMatchDto(match),
            AuthorizedParticipant = authorizedParticipant,
            CanConfirmEnded = tournamentInProgress && authorizedParticipant.HasValue &&
                !match.HasResult &&
                match.LifecycleState is not Domain.MatchLifecycleState.AdminResolutionRequired &&
                ((authorizedParticipant == Contracts.MatchParticipantSide.Participant1 && !match.Participant1Ended) ||
                 (authorizedParticipant == Contracts.MatchParticipantSide.Participant2 && !match.Participant2Ended)),
            CanSubmitScore = tournamentInProgress && authorizedParticipant.HasValue &&
                (match.LifecycleState == Domain.MatchLifecycleState.AwaitingScore ||
                 (match.LifecycleState == Domain.MatchLifecycleState.ScoreConfirmation &&
                  ((authorizedParticipant == Contracts.MatchParticipantSide.Participant1 && !match.Participant1ReportedScore1.HasValue) ||
                   (authorizedParticipant == Contracts.MatchParticipantSide.Participant2 && !match.Participant2ReportedScore1.HasValue))) ||
                 (match.LifecycleState == Domain.MatchLifecycleState.Disputed &&
                  ((authorizedParticipant == Contracts.MatchParticipantSide.Participant1 && match.Participant1CorrectionCount < 1) ||
                   (authorizedParticipant == Contracts.MatchParticipantSide.Participant2 && match.Participant2CorrectionCount < 1)))),
            CanForfeit = tournamentInProgress && authorizedParticipant.HasValue && !match.HasResult &&
                match.LifecycleState != Domain.MatchLifecycleState.AdminResolutionRequired,
            CanResolve = canResolve,
            ResolveBlockedReason = resolveBlockedReason,
            CanForceForfeit = canForceForfeit,
            ForceForfeitBlockedReason = forceForfeitBlockedReason,
            CanReverse = canReverse,
            ReverseBlockedReason = reverseBlockedReason,
            Participant1ReportedScore1 = canViewPrivateReports || authorizedParticipant.HasValue
                ? match.Participant1ReportedScore1
                : null,
            Participant1ReportedScore2 = canViewPrivateReports || authorizedParticipant.HasValue
                ? match.Participant1ReportedScore2
                : null,
            Participant2ReportedScore1 = canViewPrivateReports || authorizedParticipant.HasValue
                ? match.Participant2ReportedScore1
                : null,
            Participant2ReportedScore2 = canViewPrivateReports || authorizedParticipant.HasValue
                ? match.Participant2ReportedScore2
                : null
        };
    }

    internal GetPlacementDTO ToGetPlacementDto(
        Placement placement,
        RegistrationMappingContext context)
    {
        return new GetPlacementDTO
        {
            Place = placement.Place,
            Users = placement.Users
                .Select(user => GetPublicUser(user.UserId, null, null, context))
                .ToList(),
            Teams = placement.Teams
                .Select(team => context.Teams.TryGetValue(new TeamId(team.TeamId), out var snapshot)
                    ? ToTeamParticipantDto(snapshot)
                    : new TeamParticipantDTO { Id = team.TeamId })
                .ToList()
        };
    }

    private TeamParticipantDTO GetTeamParticipant(
        TournamentRegistration registration,
        RegistrationMappingContext context)
    {
        if (registration.TeamId.HasValue &&
            context.Teams.TryGetValue(new TeamId(registration.TeamId.Value), out var snapshot))
        {
            return ToTeamParticipantDto(snapshot);
        }

        return new TeamParticipantDTO
        {
            Id = registration.TeamId ?? Guid.Empty,
            Name = registration.TeamNameAtRegistration ?? string.Empty,
            CaptainUserId = registration.TeamCaptainUserIdAtRegistration ?? Guid.Empty,
            LogoUrl = registration.TeamLogoUrlAtRegistration
        };
    }

    private PublicUserDTO GetPublicUser(
        Guid userId,
        string? usernameSnapshot,
        string? displayNameSnapshot,
        RegistrationMappingContext context)
    {
        if (context.Users.TryGetValue(new UserId(userId), out var profile))
            return new PublicUserDTO(profile);

        var username = string.IsNullOrWhiteSpace(usernameSnapshot) ? "Incomplete profile" : usernameSnapshot;
        return new PublicUserDTO
        {
            Id = userId,
            Username = username,
            DisplayName = username
        };
    }

    private static TeamParticipantDTO ToTeamParticipantDto(TeamRosterSnapshot team)
    {
        return new TeamParticipantDTO
        {
            Id = team.TeamId.Value,
            Name = team.TeamName,
            CaptainUserId = team.CaptainUserId?.Value ?? Guid.Empty,
            LogoUrl = team.LogoUrl,
            Members = team.Members
                .Select(member => new PublicUserDTO
                {
                    Id = member.UserId.Value,
                    Username = string.IsNullOrWhiteSpace(member.Username) ? "Incomplete profile" : member.Username,
                    DisplayName = string.IsNullOrWhiteSpace(member.Username) ? "Incomplete profile" : member.Username
                })
                .ToList()
        };
    }

    private static GetTournamentSponsorPlacementDTO ToSponsorPlacementDto(SponsorPlacementSummary placement)
    {
        return new GetTournamentSponsorPlacementDTO
        {
            Id = placement.Id.Value,
            SponsorId = placement.Sponsor.Id.Value,
            SponsorName = placement.Sponsor.Name,
            SponsorTier = placement.Sponsor.SponsorTier,
            SponsorLogoUrl = placement.Sponsor.LogoUrl,
            SponsorInfoUrl = placement.Sponsor.InfoUrl,
            SponsorDescription = placement.Sponsor.Description,
            Context = placement.Context,
            Headline = placement.Headline,
            SupportLine = placement.SupportLine,
            DisplayOrder = placement.DisplayOrder
        };
    }
}
