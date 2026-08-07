using Mercurius.Modules.Competition.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Competition.Infrastructure;

internal sealed class PlacementTeamConfiguration : IEntityTypeConfiguration<PlacementTeam>
{
    public void Configure(EntityTypeBuilder<PlacementTeam> entity)
    {
        entity.ToTable("placement_teams", "competition");
        entity.HasKey(placementTeam => new { placementTeam.PlacementId, placementTeam.TeamId });
        entity.HasOne(placementTeam => placementTeam.Placement)
            .WithMany(placement => placement.Teams)
            .HasForeignKey(placementTeam => placementTeam.PlacementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
