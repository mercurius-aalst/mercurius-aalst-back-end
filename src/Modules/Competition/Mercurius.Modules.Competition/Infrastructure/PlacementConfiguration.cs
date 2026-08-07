using Mercurius.Modules.Competition.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Competition.Infrastructure;

internal sealed class PlacementConfiguration : IEntityTypeConfiguration<Placement>
{
    public void Configure(EntityTypeBuilder<Placement> entity)
    {
        entity.ToTable("placements", "competition");
        entity.HasKey(placement => placement.Id);
        entity.HasOne(placement => placement.Game)
            .WithMany(game => game.Placements)
            .HasForeignKey(placement => placement.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
