using Mercurius.Modules.Tournament.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Tournament.Infrastructure;

internal sealed class PlacementConfiguration : IEntityTypeConfiguration<Placement>
{
    public void Configure(EntityTypeBuilder<Placement> entity)
    {
        entity.ToTable("placements", "tournament");
        entity.HasKey(placement => placement.Id);
        entity.HasOne(placement => placement.Tournament)
            .WithMany(tournament => tournament.Placements)
            .HasForeignKey(placement => placement.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
