using Mercurius.Modules.Competition.Application.DTOs.Games;
using Mercurius.Modules.Competition.Application.DTOs.Matches;
using Mercurius.Modules.Competition.Application.DTOs.Participants;
using Mercurius.Modules.Competition.Application.DTOs.Placements;
using Mercurius.Modules.Competition.Application.DTOs.Registrations;
using Mercurius.Modules.Competition.Domain;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.Modules.Teams.Contracts;

namespace Mercurius.Modules.Competition.Application;

internal sealed class CompetitionDtoMapper
{
    private readonly RegistrationMappingContextBuilder _contextBuilder;
    private readonly ISponsorshipModule _sponsorshipModule;

    public CompetitionDtoMapper(
        RegistrationMappingContextBuilder contextBuilder,
        ISponsorshipModule sponsorshipModule)
    {
        _contextBuilder = contextBuilder;
        _sponsorshipModule = sponsorshipModule;
    }

    public async Task<GetGameDTO> ToGetGameDtoAsync(Game game, CancellationToken cancellationToken)
    {
        var sponsorPlacement = await _sponsorshipModule.GetSponsorPlacementAsync(
            new GameId(game.Id),
            cancellationToken);
        var context = await _contextBuilder.BuildAsync(
            game.TournamentRegistrations.ToList(),
            game.Placements.ToList(),
            cancellationToken);

        return ToGetGameDto(game, context, sponsorPlacement);
    }

    public async Task<IReadOnlyList<GetGameDTO>> ToGetGameDtosAsync(
        IReadOnlyCollection<Game> games,
        CancellationToken cancellationToken)
    {
        if (games.Count == 0)
            return [];

        var registrations = games
            .SelectMany(game => game.TournamentRegistrations)
            .ToList();
        var placements = games
            .SelectMany(game => game.Placements)
            .ToList();
        var gameIds = games
            .Select(game => new GameId(game.Id))
            .ToArray();

        var context = registrations.Count == 0 && placements.Count == 0
            ? new RegistrationMappingContext(
                new Dictionary<UserId, UserProfileSummary>(),
                new Dictionary<TeamId, TeamRosterSnapshot>())
            : await _contextBuilder.BuildAsync(registrations, placements, cancellationToken);
        var sponsorPlacements = await _sponsorshipModule.GetSponsorPlacementsAsync(gameIds, cancellationToken);

        return games
            .Select(game => ToGetGameDto(
                game,
                context,
                sponsorPlacements.GetValueOrDefault(new GameId(game.Id))))
            .ToList();
    }

    private GetGameDTO ToGetGameDto(
        Game game,
        RegistrationMappingContext context,
        SponsorPlacementSummary? sponsorPlacement)
    {

        return new GetGameDTO
        {
            Id = game.Id,
            Name = game.Name,
            StartTime = game.StartTime,
            EndTime = game.EndTime,
            PlannedStartTime = game.PlannedStartTime,
            AverageGameDurationMinutes = game.AverageGameDurationMinutes,
            RoundBreakDurationMinutes = game.RoundBreakDurationMinutes,
            EstimatedEndTime = game.EstimatedEndTime,
            Status = (Contracts.GameStatus)game.Status,
            BracketType = (Contracts.BracketType)game.BracketType,
            Format = (Contracts.GameFormat)game.Format,
            FinalsFormat = (Contracts.GameFormat)game.FinalsFormat,
            ParticipationMode = (Contracts.ParticipationMode)game.ParticipationMode,
            TeamSize = game.TeamSize,
            ImageUrl = game.ImageUrl,
            Placements = game.Placements
                .OrderBy(placement => placement.Place)
                .Select(placement => ToGetPlacementDto(placement, context))
                .ToList(),
            SponsorPlacement = sponsorPlacement is null ? null : ToSponsorPlacementDto(sponsorPlacement),
            Matches = game.Matches
                .OrderBy(match => match.RoundNumber)
                .ThenBy(match => match.MatchNumber)
                .Select(ToGetMatchDto)
                .ToList(),
            Registrations = game.TournamentRegistrations
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
                    GameId = source.GameId,
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
            GameId = registration.GameId,
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
            GameId = registration.GameId,
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

    internal static GetMatchDTO ToGetMatchDto(Match match)
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
            GameId = match.GameId,
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
            LoserNextMatchId = match.LoserNextMatchId
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

    private static GetGameSponsorPlacementDTO ToSponsorPlacementDto(SponsorPlacementSummary placement)
    {
        return new GetGameSponsorPlacementDTO
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
