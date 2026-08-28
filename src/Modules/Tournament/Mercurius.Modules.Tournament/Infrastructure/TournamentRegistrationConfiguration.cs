using Mercurius.Modules.Tournament.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Tournament.Infrastructure;

internal sealed class TournamentRegistrationConfiguration : IEntityTypeConfiguration<TournamentRegistration>
{
    public void Configure(EntityTypeBuilder<TournamentRegistration> entity)
    {
        entity.ToTable("tournament_registrations", "tournament");
        entity.HasKey(registration => registration.Id);
        entity.Property(registration => registration.Kind).IsRequired();
        entity.Property(registration => registration.Status).IsRequired();
        entity.Property(registration => registration.RegisteredByUsernameAtRegistration).HasMaxLength(32);
        entity.Property(registration => registration.UsernameAtRegistration).HasMaxLength(32);
        entity.Property(registration => registration.TeamNameAtRegistration).HasMaxLength(100);
        entity.Property(registration => registration.TeamLogoUrlAtRegistration).HasMaxLength(260);
        entity.Property(registration => registration.CreatedAtUtc).IsRequired();
        entity.Property(registration => registration.UpdatedAtUtc).IsRequired();
        entity.HasOne(registration => registration.Tournament)
            .WithMany(tournament => tournament.TournamentRegistrations)
            .HasForeignKey(registration => registration.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(registration => new { registration.TournamentId, registration.UserId })
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL")
            .HasDatabaseName("IX_TournamentRegistrations_TournamentId_UserId_PendingActive");
        entity.HasIndex(registration => new { registration.TournamentId, registration.TeamId })
            .IsUnique()
            .HasFilter("\"TeamId\" IS NOT NULL")
            .HasDatabaseName("IX_TournamentRegistrations_TournamentId_TeamId_PendingActive");
        entity.HasIndex(registration => new { registration.TournamentId, registration.RegisteredByUserId })
            .HasDatabaseName("IX_TournamentRegistrations_TournamentId_RegisteredBy_PendingActive");
        entity.HasIndex(registration => new { registration.TournamentId, registration.Status, registration.Kind });
    }
}
