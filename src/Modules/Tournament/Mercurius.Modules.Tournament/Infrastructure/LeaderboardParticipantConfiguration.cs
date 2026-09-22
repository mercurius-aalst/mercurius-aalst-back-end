using Mercurius.Modules.Tournament.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Tournament.Infrastructure;

internal sealed class LeaderboardParticipantConfiguration : IEntityTypeConfiguration<LeaderboardParticipant>
{
    public void Configure(EntityTypeBuilder<LeaderboardParticipant> entity)
    {
        entity.ToTable("leaderboard_participants", "tournament");
        entity.HasKey(participant => participant.Id);
        entity.Property(participant => participant.DisplayName).HasMaxLength(100).IsRequired();
        entity.HasOne(participant => participant.Tournament)
            .WithMany(tournament => tournament.LeaderboardParticipants)
            .HasForeignKey(participant => participant.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(participant => new { participant.TournamentId, participant.LinkedUserId })
            .IsUnique()
            .HasFilter("\"LinkedUserId\" IS NOT NULL");
    }
}

internal sealed class LeaderboardAttemptConfiguration : IEntityTypeConfiguration<LeaderboardAttempt>
{
    public void Configure(EntityTypeBuilder<LeaderboardAttempt> entity)
    {
        entity.ToTable("leaderboard_attempts", "tournament");
        entity.HasKey(attempt => attempt.Id);
        entity.Property(attempt => attempt.Score).HasPrecision(18, 6);
        entity.Property(attempt => attempt.RowVersion).IsConcurrencyToken();
        entity.ToTable(table => table.HasCheckConstraint(
            "CK_leaderboard_attempts_metric_value",
            "(\"Score\" IS NOT NULL AND \"DurationMilliseconds\" IS NULL AND \"Score\" >= 0) OR (\"Score\" IS NULL AND \"DurationMilliseconds\" IS NOT NULL AND \"DurationMilliseconds\" > 0)"));
        entity.HasOne(attempt => attempt.Participant)
            .WithMany(participant => participant.Attempts)
            .HasForeignKey(attempt => attempt.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(attempt => attempt.ParticipantId);
    }
}

internal sealed class PlacementLeaderboardParticipantConfiguration : IEntityTypeConfiguration<PlacementLeaderboardParticipant>
{
    public void Configure(EntityTypeBuilder<PlacementLeaderboardParticipant> entity)
    {
        entity.ToTable("placement_leaderboard_participants", "tournament");
        entity.HasKey(link => new { link.PlacementId, link.LeaderboardParticipantId });
        entity.HasOne(link => link.Placement)
            .WithMany(placement => placement.LeaderboardParticipants)
            .HasForeignKey(link => link.PlacementId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(link => link.LeaderboardParticipant)
            .WithMany()
            .HasForeignKey(link => link.LeaderboardParticipantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
