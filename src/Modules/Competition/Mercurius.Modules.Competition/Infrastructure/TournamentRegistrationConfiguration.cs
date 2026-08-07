using Mercurius.Modules.Competition.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Competition.Infrastructure;

internal sealed class TournamentRegistrationConfiguration : IEntityTypeConfiguration<TournamentRegistration>
{
    public void Configure(EntityTypeBuilder<TournamentRegistration> entity)
    {
        entity.ToTable("tournament_registrations", "competition");
        entity.HasKey(registration => registration.Id);
        entity.Property(registration => registration.Kind).IsRequired();
        entity.Property(registration => registration.Status).IsRequired();
        entity.Property(registration => registration.RegisteredByUsernameAtRegistration).HasMaxLength(32);
        entity.Property(registration => registration.UsernameAtRegistration).HasMaxLength(32);
        entity.Property(registration => registration.TeamNameAtRegistration).HasMaxLength(100);
        entity.Property(registration => registration.TeamLogoUrlAtRegistration).HasMaxLength(260);
        entity.Property(registration => registration.CreatedAtUtc).IsRequired();
        entity.Property(registration => registration.UpdatedAtUtc).IsRequired();
        entity.HasOne(registration => registration.Game)
            .WithMany(game => game.TournamentRegistrations)
            .HasForeignKey(registration => registration.GameId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(registration => new { registration.GameId, registration.UserId })
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL")
            .HasDatabaseName("IX_TournamentRegistrations_GameId_UserId_PendingActive");
        entity.HasIndex(registration => new { registration.GameId, registration.TeamId })
            .IsUnique()
            .HasFilter("\"TeamId\" IS NOT NULL")
            .HasDatabaseName("IX_TournamentRegistrations_GameId_TeamId_PendingActive");
        entity.HasIndex(registration => new { registration.GameId, registration.RegisteredByUserId })
            .HasDatabaseName("IX_TournamentRegistrations_GameId_RegisteredBy_PendingActive");
        entity.HasIndex(registration => new { registration.GameId, registration.Status, registration.Kind });
    }
}
