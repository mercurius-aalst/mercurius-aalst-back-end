using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.Modules.Sponsorship.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Mercurius.Modules.Sponsorship.Infrastructure;

internal sealed class TournamentSponsorPlacementConfiguration : IEntityTypeConfiguration<TournamentSponsorPlacement>
{
    public void Configure(EntityTypeBuilder<TournamentSponsorPlacement> entity)
    {
        var sponsorContextConverter = new EnumToStringConverter<SponsorContext>();

        entity.ToTable("tournament_sponsor_placements", "sponsorship");
        entity.HasKey(placement => placement.Id);
        entity.Property(placement => placement.Context)
            .HasConversion(sponsorContextConverter)
            .IsRequired();
        entity.Property(placement => placement.Headline).HasMaxLength(160);
        entity.Property(placement => placement.SupportLine).HasMaxLength(220);
        entity.Property(placement => placement.DisplayOrder).IsRequired();
        entity.HasOne(placement => placement.Sponsor)
            .WithMany(sponsor => sponsor.TournamentSponsorPlacements)
            .HasForeignKey(placement => placement.SponsorId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(placement => placement.TournamentId).IsUnique();
    }
}
