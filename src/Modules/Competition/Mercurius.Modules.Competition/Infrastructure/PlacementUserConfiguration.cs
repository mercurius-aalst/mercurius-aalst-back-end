using Mercurius.Modules.Competition.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Competition.Infrastructure;

internal sealed class PlacementUserConfiguration : IEntityTypeConfiguration<PlacementUser>
{
    public void Configure(EntityTypeBuilder<PlacementUser> entity)
    {
        entity.ToTable("placement_users", "competition");
        entity.HasKey(placementUser => new { placementUser.PlacementId, placementUser.UserId });
        entity.HasOne(placementUser => placementUser.Placement)
            .WithMany(placement => placement.Users)
            .HasForeignKey(placementUser => placementUser.PlacementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
