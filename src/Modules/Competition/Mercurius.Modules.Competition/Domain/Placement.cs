namespace Mercurius.Modules.Competition.Domain;

internal sealed class Placement
{
    public Guid Id { get; set; }
    public int Place { get; set; }
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    public IList<PlacementUser> Users { get; set; } = [];
    public IList<PlacementTeam> Teams { get; set; } = [];
}

internal sealed class PlacementUser
{
    public Guid PlacementId { get; set; }
    public Placement Placement { get; set; } = null!;
    public Guid UserId { get; set; }
}

internal sealed class PlacementTeam
{
    public Guid PlacementId { get; set; }
    public Placement Placement { get; set; } = null!;
    public Guid TeamId { get; set; }
}
