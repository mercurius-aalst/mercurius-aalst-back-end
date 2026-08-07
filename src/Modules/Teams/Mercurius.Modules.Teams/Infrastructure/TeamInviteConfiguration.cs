using Mercurius.Modules.Teams.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Teams.Infrastructure;

internal sealed class TeamInviteConfiguration : IEntityTypeConfiguration<TeamInvite>
{
    public void Configure(EntityTypeBuilder<TeamInvite> entity)
    {
        entity.ToTable("team_invites", "teams");
        entity.HasKey(invite => invite.Id);
        entity.HasOne(invite => invite.Team)
            .WithMany(team => team.TeamInvites)
            .HasForeignKey(invite => invite.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.Property(invite => invite.Status).IsRequired();
        entity.Property(invite => invite.CreatedAt).IsRequired();
        entity.Property(invite => invite.ExpiresAt).IsRequired();
        entity.HasIndex(invite => new { invite.TeamId, invite.UserId })
            .IsUnique()
            .HasFilter("\"Status\" = 0")
            .HasDatabaseName("IX_TeamInvites_TeamId_UserId_Pending");
        entity.HasIndex(invite => new { invite.UserId, invite.Status, invite.ExpiresAt });
        entity.HasIndex(invite => new { invite.TeamId, invite.Status, invite.ExpiresAt });
    }
}
