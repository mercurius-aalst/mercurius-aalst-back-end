using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.Public;

public static class PublicReadModelProjections
{
    public static IQueryable<PublicGameSummaryDTO> SelectPublicGameSummaries(
        this IQueryable<Game> query,
        PublicAudience audience)
    {
        var includePlatformIds = audience == PublicAudience.Authenticated;

        return query.Select(game => new PublicGameSummaryDTO
        {
            Id = game.Id,
            Name = game.Name,
            StartTime = game.StartTime,
            EndTime = game.EndTime,
            Status = game.Status,
            BracketType = game.BracketType,
            Format = game.Format,
            FinalsFormat = game.FinalsFormat,
            ParticipationMode = game.ParticipationMode,
            ImageUrl = game.ImageUrl,
            RegisterFormUrl = game.RegisterFormUrl,
            SponsorPlacement = game.SponsorPlacement == null
                ? null
                : new GetGameSponsorPlacementDTO
                {
                    Id = game.SponsorPlacement.Id,
                    SponsorId = game.SponsorPlacement.SponsorId,
                    SponsorName = game.SponsorPlacement.Sponsor.Name,
                    SponsorTier = game.SponsorPlacement.Sponsor.SponsorTier,
                    SponsorLogoUrl = game.SponsorPlacement.Sponsor.LogoUrl,
                    SponsorInfoUrl = game.SponsorPlacement.Sponsor.InfoUrl,
                    SponsorDescription = game.SponsorPlacement.Sponsor.Description,
                    Context = game.SponsorPlacement.Context,
                    Headline = game.SponsorPlacement.Headline,
                    SupportLine = game.SponsorPlacement.SupportLine,
                    DisplayOrder = game.SponsorPlacement.DisplayOrder
                },
            Users = game.ParticipationMode == ParticipationMode.Individual
                ? game.RegisteredUsers.Select(user => new PublicParticipantDTO
                {
                    Id = user.Id,
                    Username = string.IsNullOrWhiteSpace(user.Username) ? "Incomplete profile" : user.Username,
                    DisplayName = string.IsNullOrWhiteSpace(user.Username) ? "Incomplete profile" : user.Username,
                    DiscordId = includePlatformIds && !string.IsNullOrWhiteSpace(user.DiscordId) ? user.DiscordId : null,
                    SteamId = includePlatformIds && !string.IsNullOrWhiteSpace(user.SteamId) ? user.SteamId : null,
                    RiotId = includePlatformIds && !string.IsNullOrWhiteSpace(user.RiotId) ? user.RiotId : null
                }).ToList()
                : new List<PublicParticipantDTO>(),
            Teams = game.ParticipationMode == ParticipationMode.Team
                ? game.RegisteredTeams.Select(team => new PublicTeamDTO
                {
                    Id = team.Id,
                    Name = team.Name,
                    CaptainUserId = team.CaptainUserId,
                    Members = team.Members.Select(member => new PublicParticipantDTO
                    {
                        Id = member.Id,
                        Username = string.IsNullOrWhiteSpace(member.Username) ? "Incomplete profile" : member.Username,
                        DisplayName = string.IsNullOrWhiteSpace(member.Username) ? "Incomplete profile" : member.Username,
                        DiscordId = includePlatformIds && !string.IsNullOrWhiteSpace(member.DiscordId) ? member.DiscordId : null,
                        SteamId = includePlatformIds && !string.IsNullOrWhiteSpace(member.SteamId) ? member.SteamId : null,
                        RiotId = includePlatformIds && !string.IsNullOrWhiteSpace(member.RiotId) ? member.RiotId : null
                    }).ToList()
                }).ToList()
                : new List<PublicTeamDTO>()
        });
    }

    public static IQueryable<PublicGameDetailDTO> SelectPublicGameDetails(
        this IQueryable<Game> query,
        PublicAudience audience)
    {
        var includePlatformIds = audience == PublicAudience.Authenticated;

        return query.Select(game => new PublicGameDetailDTO
        {
            Id = game.Id,
            Name = game.Name,
            StartTime = game.StartTime,
            EndTime = game.EndTime,
            Status = game.Status,
            BracketType = game.BracketType,
            Format = game.Format,
            FinalsFormat = game.FinalsFormat,
            ParticipationMode = game.ParticipationMode,
            ImageUrl = game.ImageUrl,
            RegisterFormUrl = game.RegisterFormUrl,
            SponsorPlacement = game.SponsorPlacement == null
                ? null
                : new GetGameSponsorPlacementDTO
                {
                    Id = game.SponsorPlacement.Id,
                    SponsorId = game.SponsorPlacement.SponsorId,
                    SponsorName = game.SponsorPlacement.Sponsor.Name,
                    SponsorTier = game.SponsorPlacement.Sponsor.SponsorTier,
                    SponsorLogoUrl = game.SponsorPlacement.Sponsor.LogoUrl,
                    SponsorInfoUrl = game.SponsorPlacement.Sponsor.InfoUrl,
                    SponsorDescription = game.SponsorPlacement.Sponsor.Description,
                    Context = game.SponsorPlacement.Context,
                    Headline = game.SponsorPlacement.Headline,
                    SupportLine = game.SponsorPlacement.SupportLine,
                    DisplayOrder = game.SponsorPlacement.DisplayOrder
                },
            Users = game.ParticipationMode == ParticipationMode.Individual
                ? game.RegisteredUsers.Select(user => new PublicParticipantDTO
                {
                    Id = user.Id,
                    Username = string.IsNullOrWhiteSpace(user.Username) ? "Incomplete profile" : user.Username,
                    DisplayName = string.IsNullOrWhiteSpace(user.Username) ? "Incomplete profile" : user.Username,
                    DiscordId = includePlatformIds && !string.IsNullOrWhiteSpace(user.DiscordId) ? user.DiscordId : null,
                    SteamId = includePlatformIds && !string.IsNullOrWhiteSpace(user.SteamId) ? user.SteamId : null,
                    RiotId = includePlatformIds && !string.IsNullOrWhiteSpace(user.RiotId) ? user.RiotId : null
                }).ToList()
                : new List<PublicParticipantDTO>(),
            Teams = game.ParticipationMode == ParticipationMode.Team
                ? game.RegisteredTeams.Select(team => new PublicTeamDTO
                {
                    Id = team.Id,
                    Name = team.Name,
                    CaptainUserId = team.CaptainUserId,
                    Members = team.Members.Select(member => new PublicParticipantDTO
                    {
                        Id = member.Id,
                        Username = string.IsNullOrWhiteSpace(member.Username) ? "Incomplete profile" : member.Username,
                        DisplayName = string.IsNullOrWhiteSpace(member.Username) ? "Incomplete profile" : member.Username,
                        DiscordId = includePlatformIds && !string.IsNullOrWhiteSpace(member.DiscordId) ? member.DiscordId : null,
                        SteamId = includePlatformIds && !string.IsNullOrWhiteSpace(member.SteamId) ? member.SteamId : null,
                        RiotId = includePlatformIds && !string.IsNullOrWhiteSpace(member.RiotId) ? member.RiotId : null
                    }).ToList()
                }).ToList()
                : new List<PublicTeamDTO>(),
            Placements = game.Placements.Select(placement => new PublicPlacementDTO
            {
                Place = placement.Place,
                Users = game.ParticipationMode == ParticipationMode.Individual
                    ? placement.Users.Select(user => new PublicParticipantDTO
                    {
                        Id = user.Id,
                        Username = string.IsNullOrWhiteSpace(user.Username) ? "Incomplete profile" : user.Username,
                        DisplayName = string.IsNullOrWhiteSpace(user.Username) ? "Incomplete profile" : user.Username,
                        DiscordId = includePlatformIds && !string.IsNullOrWhiteSpace(user.DiscordId) ? user.DiscordId : null,
                        SteamId = includePlatformIds && !string.IsNullOrWhiteSpace(user.SteamId) ? user.SteamId : null,
                        RiotId = includePlatformIds && !string.IsNullOrWhiteSpace(user.RiotId) ? user.RiotId : null
                    }).ToList()
                    : new List<PublicParticipantDTO>(),
                Teams = game.ParticipationMode == ParticipationMode.Team
                    ? placement.Teams.Select(team => new PublicTeamDTO
                    {
                        Id = team.Id,
                        Name = team.Name,
                        CaptainUserId = team.CaptainUserId,
                        Members = team.Members.Select(member => new PublicParticipantDTO
                        {
                            Id = member.Id,
                            Username = string.IsNullOrWhiteSpace(member.Username) ? "Incomplete profile" : member.Username,
                            DisplayName = string.IsNullOrWhiteSpace(member.Username) ? "Incomplete profile" : member.Username,
                            DiscordId = includePlatformIds && !string.IsNullOrWhiteSpace(member.DiscordId) ? member.DiscordId : null,
                            SteamId = includePlatformIds && !string.IsNullOrWhiteSpace(member.SteamId) ? member.SteamId : null,
                            RiotId = includePlatformIds && !string.IsNullOrWhiteSpace(member.RiotId) ? member.RiotId : null
                        }).ToList()
                    }).ToList()
                    : new List<PublicTeamDTO>()
            }).ToList(),
            Matches = game.Matches.Select(match => new GetMatchDTO
            {
                Id = match.Id,
                StartTime = match.StartTime,
                EndTime = match.EndTime,
                BracketType = match.BracketType,
                Format = match.Format,
                ParticipationMode = match.ParticipationMode,
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
            }).ToList()
        });
    }

    public static IQueryable<PublicTeamDTO> SelectPublicTeams(
        this IQueryable<Team> query,
        PublicAudience audience)
    {
        var includePlatformIds = audience == PublicAudience.Authenticated;

        return query.Select(team => new PublicTeamDTO
        {
            Id = team.Id,
            Name = team.Name,
            CaptainUserId = team.CaptainUserId,
            Members = team.Members.Select(member => new PublicParticipantDTO
            {
                Id = member.Id,
                Username = string.IsNullOrWhiteSpace(member.Username) ? "Incomplete profile" : member.Username,
                DisplayName = string.IsNullOrWhiteSpace(member.Username) ? "Incomplete profile" : member.Username,
                DiscordId = includePlatformIds && !string.IsNullOrWhiteSpace(member.DiscordId) ? member.DiscordId : null,
                SteamId = includePlatformIds && !string.IsNullOrWhiteSpace(member.SteamId) ? member.SteamId : null,
                RiotId = includePlatformIds && !string.IsNullOrWhiteSpace(member.RiotId) ? member.RiotId : null
            }).ToList()
        });
    }
}
