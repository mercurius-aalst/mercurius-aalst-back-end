using Mercurius.Modules.Competition.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Competition.Infrastructure;

internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> entity)
    {
        entity.ToTable("games", "competition");
        entity.HasKey(game => game.Id);
        entity.Property(game => game.Name).IsRequired();
        entity.Property(game => game.StartTime).IsRequired();
        entity.Property(game => game.EndTime).IsRequired();
        entity.Property(game => game.PlannedStartTime).IsRequired();
        entity.Property(game => game.AverageGameDurationMinutes).IsRequired();
        entity.Property(game => game.RoundBreakDurationMinutes).IsRequired();
        entity.Property(game => game.EstimatedEndTime).IsRequired(false);
        entity.Property(game => game.TeamSize).IsRequired(false);
    }
}
