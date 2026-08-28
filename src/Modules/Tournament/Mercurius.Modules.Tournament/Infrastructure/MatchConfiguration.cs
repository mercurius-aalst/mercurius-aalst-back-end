using Mercurius.Modules.Tournament.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Tournament.Infrastructure;

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> entity)
    {
        entity.ToTable("matches", "tournament");
        entity.HasKey(match => match.Id);
        entity.Property(match => match.EstimatedStartTime).IsRequired(false);
        entity.Property(match => match.EstimatedEndTime).IsRequired(false);
        entity.HasOne(match => match.Tournament)
            .WithMany(tournament => tournament.Matches)
            .HasForeignKey(match => match.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(match => match.WinnerNextMatch)
            .WithMany()
            .HasForeignKey(match => match.WinnerNextMatchId)
            .IsRequired(false);
        entity.HasOne(match => match.LoserNextMatch)
            .WithMany()
            .HasForeignKey(match => match.LoserNextMatchId)
            .IsRequired(false);
    }
}
