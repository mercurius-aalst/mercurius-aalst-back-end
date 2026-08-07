using Mercurius.Modules.Competition.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Competition.Infrastructure;

internal sealed class TournamentRegistrationRosterMemberConfiguration : IEntityTypeConfiguration<TournamentRegistrationRosterMember>
{
    public void Configure(EntityTypeBuilder<TournamentRegistrationRosterMember> entity)
    {
        entity.ToTable("roster_members", "competition");
        entity.HasKey(member => member.Id);
        entity.Property(member => member.ConfirmationStatus).IsRequired();
        entity.Property(member => member.UsernameAtRegistration).HasMaxLength(32).IsRequired();
        entity.Property(member => member.DisplayNameAtRegistration).HasMaxLength(200).IsRequired();
        entity.Property(member => member.TeamNameAtRegistration).HasMaxLength(100);
        entity.Property(member => member.CreatedAtUtc).IsRequired();
        entity.Property(member => member.UpdatedAtUtc).IsRequired();
        entity.HasOne(member => member.TournamentRegistration)
            .WithMany(registration => registration.RosterMembers)
            .HasForeignKey(member => member.TournamentRegistrationId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(member => member.Game)
            .WithMany()
            .HasForeignKey(member => member.GameId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(member => new { member.GameId, member.UserId })
            .IsUnique()
            .HasDatabaseName("IX_TournamentRosterMembers_GameId_UserId_PendingActive");
        entity.HasIndex(member => new { member.GameId, member.TeamId, member.UserId });
        entity.HasIndex(member => new { member.TournamentRegistrationId, member.ConfirmationStatus });
    }
}
