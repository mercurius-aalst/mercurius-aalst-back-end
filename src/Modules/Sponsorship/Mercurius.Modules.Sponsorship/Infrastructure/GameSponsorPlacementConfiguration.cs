using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.Modules.Sponsorship.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Mercurius.Modules.Sponsorship.Infrastructure;

internal sealed class GameSponsorPlacementConfiguration : IEntityTypeConfiguration<GameSponsorPlacement>
{
    public void Configure(EntityTypeBuilder<GameSponsorPlacement> entity)
    {
        var sponsorContextConverter = new EnumToStringConverter<SponsorContext>();

        entity.ToTable("game_sponsor_placements", "sponsorship");
        entity.HasKey(placement => placement.Id);
        entity.Property(placement => placement.Context)
            .HasConversion(sponsorContextConverter)
            .IsRequired();
        entity.Property(placement => placement.Headline).HasMaxLength(160);
        entity.Property(placement => placement.SupportLine).HasMaxLength(220);
        entity.Property(placement => placement.DisplayOrder).IsRequired();
        entity.HasOne(placement => placement.Sponsor)
            .WithMany(sponsor => sponsor.GameSponsorPlacements)
            .HasForeignKey(placement => placement.SponsorId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(placement => placement.GameId).IsUnique();
    }
}
