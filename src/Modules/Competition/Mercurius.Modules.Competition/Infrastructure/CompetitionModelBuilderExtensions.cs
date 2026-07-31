using Mercurius.Modules.Competition.Domain;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Competition.Infrastructure;

internal static class CompetitionModelBuilderExtensions
{
    internal static ModelBuilder ApplyCompetitionConfiguration<TUser, TTeam, TGameSponsorPlacement>(
        this ModelBuilder modelBuilder)
        where TUser : class
        where TTeam : class
        where TGameSponsorPlacement : class
    {
        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("Games");
            entity.HasKey(game => game.Id);
            entity.Property(game => game.Name).IsRequired();
            entity.Property(game => game.StartTime).IsRequired();
            entity.Property(game => game.EndTime).IsRequired();
            entity.Property(game => game.PlannedStartTime).IsRequired();
            entity.Property(game => game.AverageGameDurationMinutes).IsRequired();
            entity.Property(game => game.RoundBreakDurationMinutes).IsRequired();
            entity.Property(game => game.EstimatedEndTime).IsRequired(false);
            entity.Property(game => game.TeamSize).IsRequired(false);
            entity.HasOne<TGameSponsorPlacement>()
                .WithOne()
                .HasForeignKey<TGameSponsorPlacement>("GameId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.ToTable("Matches");
            entity.HasKey(match => match.Id);
            entity.Property(match => match.EstimatedStartTime).IsRequired(false);
            entity.Property(match => match.EstimatedEndTime).IsRequired(false);
            ConfigureOptionalReference<TUser, Match>(entity, nameof(Match.UserParticipant1Id));
            ConfigureOptionalReference<TUser, Match>(entity, nameof(Match.UserParticipant2Id));
            ConfigureOptionalReference<TUser, Match>(entity, nameof(Match.UserWinnerId));
            ConfigureOptionalReference<TUser, Match>(entity, nameof(Match.UserLoserId));
            ConfigureOptionalReference<TTeam, Match>(entity, nameof(Match.TeamParticipant1Id));
            ConfigureOptionalReference<TTeam, Match>(entity, nameof(Match.TeamParticipant2Id));
            ConfigureOptionalReference<TTeam, Match>(entity, nameof(Match.TeamWinnerId));
            ConfigureOptionalReference<TTeam, Match>(entity, nameof(Match.TeamLoserId));
            entity.HasOne(match => match.Game)
                .WithMany(game => game.Matches)
                .HasForeignKey(match => match.GameId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(match => match.WinnerNextMatch)
                .WithMany()
                .HasForeignKey(match => match.WinnerNextMatchId)
                .IsRequired(false);
            entity.HasOne(match => match.LoserNextMatch)
                .WithMany()
                .HasForeignKey(match => match.LoserNextMatchId)
                .IsRequired(false);
        });

        modelBuilder.Entity<TournamentRegistration>(entity =>
        {
            entity.ToTable("TournamentRegistrations");
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
            ConfigureRequiredReference<TUser, TournamentRegistration>(
                entity,
                nameof(TournamentRegistration.RegisteredByUserId),
                DeleteBehavior.Restrict);
            ConfigureOptionalReference<TUser, TournamentRegistration>(
                entity,
                nameof(TournamentRegistration.UserId),
                DeleteBehavior.Restrict);
            ConfigureOptionalReference<TTeam, TournamentRegistration>(
                entity,
                nameof(TournamentRegistration.TeamId),
                DeleteBehavior.Restrict);
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
        });

        modelBuilder.Entity<TournamentRegistrationRosterMember>(entity =>
        {
            entity.ToTable("TournamentRegistrationRosterMembers");
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
            ConfigureRequiredReference<TUser, TournamentRegistrationRosterMember>(
                entity,
                nameof(TournamentRegistrationRosterMember.UserId),
                DeleteBehavior.Restrict);
            ConfigureOptionalReference<TTeam, TournamentRegistrationRosterMember>(
                entity,
                nameof(TournamentRegistrationRosterMember.TeamId),
                DeleteBehavior.Restrict);
            entity.HasIndex(member => new { member.GameId, member.UserId })
                .IsUnique()
                .HasDatabaseName("IX_TournamentRosterMembers_GameId_UserId_PendingActive");
            entity.HasIndex(member => new { member.GameId, member.TeamId, member.UserId });
            entity.HasIndex(member => new { member.TournamentRegistrationId, member.ConfirmationStatus });
        });

        modelBuilder.Entity<Placement>(entity =>
        {
            entity.ToTable("Placements");
            entity.HasKey(placement => placement.Id);
            entity.HasOne(placement => placement.Game)
                .WithMany(game => game.Placements)
                .HasForeignKey(placement => placement.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlacementUser>(entity =>
        {
            entity.ToTable("PlacementUser");
            entity.HasKey(placementUser => new { placementUser.PlacementId, placementUser.UserId });
            entity.HasOne(placementUser => placementUser.Placement)
                .WithMany(placement => placement.Users)
                .HasForeignKey(placementUser => placementUser.PlacementId)
                .OnDelete(DeleteBehavior.Cascade);
            ConfigureRequiredReference<TUser, PlacementUser>(
                entity,
                nameof(PlacementUser.UserId),
                DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlacementTeam>(entity =>
        {
            entity.ToTable("PlacementTeam");
            entity.HasKey(placementTeam => new { placementTeam.PlacementId, placementTeam.TeamId });
            entity.HasOne(placementTeam => placementTeam.Placement)
                .WithMany(placement => placement.Teams)
                .HasForeignKey(placementTeam => placementTeam.PlacementId)
                .OnDelete(DeleteBehavior.Cascade);
            ConfigureRequiredReference<TTeam, PlacementTeam>(
                entity,
                nameof(PlacementTeam.TeamId),
                DeleteBehavior.Cascade);
        });

        return modelBuilder;
    }

    private static void ConfigureOptionalReference<TPrincipal, TDependent>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TDependent> entity,
        string foreignKeyProperty,
        DeleteBehavior deleteBehavior = DeleteBehavior.NoAction)
        where TPrincipal : class
        where TDependent : class
    {
        entity.HasOne<TPrincipal>()
            .WithMany()
            .HasForeignKey(foreignKeyProperty)
            .IsRequired(false)
            .OnDelete(deleteBehavior);
    }

    private static void ConfigureRequiredReference<TPrincipal, TDependent>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TDependent> entity,
        string foreignKeyProperty,
        DeleteBehavior deleteBehavior)
        where TPrincipal : class
        where TDependent : class
    {
        entity.HasOne<TPrincipal>()
            .WithMany()
            .HasForeignKey(foreignKeyProperty)
            .IsRequired()
            .OnDelete(deleteBehavior);
    }
}
