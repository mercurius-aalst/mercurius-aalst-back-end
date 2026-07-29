using Mercurius.Modules.Identity.Domain;
using Mercurius.Modules.Teams.Domain;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Teams.Infrastructure;

internal static class TeamsModelBuilderExtensions
{
    internal static ModelBuilder ApplyTeamsConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NormalizedName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LogoUrl).HasMaxLength(260);
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.HasIndex(e => e.NormalizedName)
                  .IsUnique()
                  .HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(e => e.CaptainUserId);
            entity.HasMany(e => e.Members)
                  .WithMany()
                  .UsingEntity<Dictionary<string, object>>(
                      "TeamUser",
                      j => j.HasOne<User>()
                          .WithMany()
                          .HasForeignKey("UserId")
                          .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<Team>()
                          .WithMany()
                          .HasForeignKey("TeamId")
                          .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasKey("TeamId", "UserId"));
            entity.HasOne(e => e.Captain)
                   .WithMany()
                   .HasForeignKey(e => e.CaptainUserId)
                   .IsRequired(false);
        });

        modelBuilder.Entity<TeamInvite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Team)
                  .WithMany(t => t.TeamInvites)
                  .HasForeignKey(e => e.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.HasIndex(e => new { e.TeamId, e.UserId })
                  .IsUnique()
                  .HasFilter("\"Status\" = 0")
                  .HasDatabaseName("IX_TeamInvites_TeamId_UserId_Pending");
            entity.HasIndex(e => new { e.UserId, e.Status, e.ExpiresAt });
            entity.HasIndex(e => new { e.TeamId, e.Status, e.ExpiresAt });
        });

        return modelBuilder;
    }
}
