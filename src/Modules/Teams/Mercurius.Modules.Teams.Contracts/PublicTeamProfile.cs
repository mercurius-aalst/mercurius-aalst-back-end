namespace Mercurius.Modules.Teams.Contracts;

public sealed record PublicTeamProfile(
    string TeamName,
    string? CaptainUsername,
    string? LogoUrl,
    IReadOnlyList<PublicTeamMemberSummary> Members,
    IReadOnlyList<PublicTeamTournamentSummary> Tournaments);
