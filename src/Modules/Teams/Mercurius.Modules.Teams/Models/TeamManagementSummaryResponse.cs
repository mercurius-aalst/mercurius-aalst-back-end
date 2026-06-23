namespace Mercurius.Modules.Teams.Models;

public sealed class TeamManagementSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CaptainUserId { get; set; }
    public string? CaptainUsername { get; set; }
    public string? LogoUrl { get; set; }
    public IReadOnlyList<PublicUserResponse> Members { get; set; } = [];
}
