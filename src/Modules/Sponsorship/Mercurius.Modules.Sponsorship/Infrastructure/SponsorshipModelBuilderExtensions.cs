using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.Modules.Sponsorship.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Mercurius.Modules.Sponsorship.Infrastructure;

internal static class SponsorshipModelBuilderExtensions
{
    internal static ModelBuilder ApplySponsorshipConfiguration(this ModelBuilder modelBuilder)
    {
        var sponsorTierConverter = new EnumToStringConverter<SponsorTier>();
        var sponsorContextConverter = new EnumToStringConverter<SponsorContext>();

        modelBuilder.Entity<Sponsor>(entity =>
        {
            entity.ToTable("Sponsors");
            entity.HasKey(sponsor => sponsor.Id);
            entity.Property(sponsor => sponsor.Name).IsRequired();
            entity.Property(sponsor => sponsor.LogoUrl).IsRequired();
            entity.Property(sponsor => sponsor.InfoUrl).IsRequired();
            entity.Property(sponsor => sponsor.Description).HasMaxLength(1200);
            entity.Property(sponsor => sponsor.SponsorTier)
                .HasConversion(sponsorTierConverter)
                .IsRequired();
            entity.HasMany(sponsor => sponsor.GameSponsorPlacements)
                .WithOne(placement => placement.Sponsor)
                .HasForeignKey(placement => placement.SponsorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GameSponsorPlacement>(entity =>
        {
            entity.ToTable("GameSponsorPlacements");
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
        });

        return modelBuilder;
    }
}
