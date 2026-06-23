namespace Mercurius.Modules.Teams.Api;

public sealed class PublicTeamProfileResponse
{
    public string TeamName { get; set; } = string.Empty;
    public string? CaptainUsername { get; set; }
    public string? LogoUrl { get; set; }
    public IReadOnlyList<PublicTeamMemberResponse> Members { get; set; } = [];
    public IReadOnlyList<PublicTeamTournamentResponse> Tournaments { get; set; } = [];
}
