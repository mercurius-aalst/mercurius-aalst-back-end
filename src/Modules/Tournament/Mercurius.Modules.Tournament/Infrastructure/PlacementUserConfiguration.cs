using Mercurius.Modules.Tournament.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Tournament.Infrastructure;

internal sealed class PlacementUserConfiguration : IEntityTypeConfiguration<PlacementUser>
{
    public void Configure(EntityTypeBuilder<PlacementUser> entity)
    {
        entity.ToTable("placement_users", "tournament");
        entity.HasKey(placementUser => new { placementUser.PlacementId, placementUser.UserId });
        entity.HasOne(placementUser => placementUser.Placement)
            .WithMany(placement => placement.Users)
            .HasForeignKey(placementUser => placementUser.PlacementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
