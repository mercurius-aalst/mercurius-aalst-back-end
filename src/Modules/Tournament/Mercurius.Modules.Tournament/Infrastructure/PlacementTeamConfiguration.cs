using Mercurius.Modules.Tournament.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Tournament.Infrastructure;

internal sealed class PlacementTeamConfiguration : IEntityTypeConfiguration<PlacementTeam>
{
    public void Configure(EntityTypeBuilder<PlacementTeam> entity)
    {
        entity.ToTable("placement_teams", "tournament");
        entity.HasKey(placementTeam => new { placementTeam.PlacementId, placementTeam.TeamId });
        entity.HasOne(placementTeam => placementTeam.Placement)
            .WithMany(placement => placement.Teams)
            .HasForeignKey(placementTeam => placementTeam.PlacementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
