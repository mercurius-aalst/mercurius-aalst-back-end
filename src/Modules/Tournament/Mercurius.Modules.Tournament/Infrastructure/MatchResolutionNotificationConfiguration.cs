using Mercurius.Modules.Tournament.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Tournament.Infrastructure;

internal sealed class MatchResolutionNotificationConfiguration : IEntityTypeConfiguration<MatchResolutionNotification>
{
    public void Configure(EntityTypeBuilder<MatchResolutionNotification> entity)
    {
        entity.ToTable("match_resolution_notifications", "tournament");
        entity.HasKey(notification => notification.Id);
        entity.Property(notification => notification.RecipientKind).IsRequired();
        entity.Property(notification => notification.OccurredAtUtc).IsRequired();
        entity.Property(notification => notification.CreatedAtUtc).IsRequired();
        entity.HasIndex(notification => new
        {
            notification.TournamentId,
            notification.RecipientUserId,
            notification.CreatedAtUtc
        });
        entity.HasIndex(notification => new
        {
            notification.MatchId,
            notification.CreatedAtUtc
        });
    }
}
