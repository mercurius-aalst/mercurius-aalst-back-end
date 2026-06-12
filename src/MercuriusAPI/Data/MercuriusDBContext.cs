using Mercurius.LAN.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Mercurius.LAN.API.Data;

public partial class MercuriusDBContext : DbContext
{
    public MercuriusDBContext()
    {
    }

    public MercuriusDBContext(DbContextOptions<MercuriusDBContext> options) : base(options)
    {
    }

    public DbSet<Team> Teams { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<TeamInvite> TeamInvites { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Placement> Placements { get; set; }
    public DbSet<Sponsor> Sponsors { get; set; }
    public DbSet<GameSponsorPlacement> GameSponsorPlacements { get; set; }
    public DbSet<TournamentRegistration> TournamentRegistrations { get; set; }
    public DbSet<TournamentRegistrationRosterMember> TournamentRegistrationRosterMembers { get; set; }
    public DbSet<TournamentRosterConfirmationNotification> TournamentRosterConfirmationNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var sponsorTierConverter = new EnumToStringConverter<SponsorTier>();
        var sponsorContextConverter = new EnumToStringConverter<SponsorContext>();

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Auth0UserId).IsUnique();
            entity.HasIndex(e => e.Username)
                  .IsUnique()
                  .HasFilter("\"Username\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.HasIndex(e => e.NormalizedUsername)
                  .IsUnique()
                  .HasFilter("\"NormalizedUsername\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.HasIndex(e => e.Email)
                  .IsUnique()
                  .HasFilter("\"Email\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.Property(e => e.Auth0UserId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Username).HasMaxLength(32);
            entity.Property(e => e.NormalizedUsername).HasMaxLength(32);
            entity.Property(e => e.Firstname).HasMaxLength(100);
            entity.Property(e => e.Lastname).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.DiscordId).HasMaxLength(100);
            entity.Property(e => e.SteamId).HasMaxLength(100);
            entity.Property(e => e.RiotId).HasMaxLength(100);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NormalizedName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LogoUrl).HasMaxLength(260);
            entity.Property(e => e.IsDeleted).IsRequired();
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

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EstimatedStartTime).IsRequired(false);
            entity.Property(e => e.EstimatedEndTime).IsRequired(false);
            entity.HasOne(e => e.UserParticipant1)
                  .WithMany()
                  .HasForeignKey(e => e.UserParticipant1Id).IsRequired(false);
            entity.HasOne(e => e.UserParticipant2)
                  .WithMany()
                  .HasForeignKey(e => e.UserParticipant2Id).IsRequired(false);
            entity.HasOne(e => e.UserWinner)
                    .WithMany()
                    .HasForeignKey(e => e.UserWinnerId).IsRequired(false);
            entity.HasOne(e => e.UserLoser)
                    .WithMany()
                    .HasForeignKey(e => e.UserLoserId).IsRequired(false);
            entity.HasOne(e => e.TeamParticipant1)
                  .WithMany()
                  .HasForeignKey(e => e.TeamParticipant1Id).IsRequired(false);
            entity.HasOne(e => e.TeamParticipant2)
                  .WithMany()
                  .HasForeignKey(e => e.TeamParticipant2Id).IsRequired(false);
            entity.HasOne(e => e.TeamWinner)
                    .WithMany()
                    .HasForeignKey(e => e.TeamWinnerId).IsRequired(false);
            entity.HasOne(e => e.TeamLoser)
                    .WithMany()
                    .HasForeignKey(e => e.TeamLoserId).IsRequired(false);
            entity.HasOne(e => e.Game)
                    .WithMany(e => e.Matches)
                    .HasForeignKey(e => e.GameId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.WinnerNextMatch)
                    .WithMany()
                    .HasForeignKey(e => e.WinnerNextMatchId).IsRequired(false);
            entity.HasOne(e => e.LoserNextMatch)
                    .WithMany()
                    .HasForeignKey(e => e.LoserNextMatchId).IsRequired(false);
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.EndTime).IsRequired();
            entity.Property(e => e.PlannedStartTime).IsRequired();
            entity.Property(e => e.AverageGameDurationMinutes).IsRequired();
            entity.Property(e => e.RoundBreakDurationMinutes).IsRequired();
            entity.Property(e => e.EstimatedEndTime).IsRequired(false);
            entity.Property(e => e.TeamSize).IsRequired(false);
            entity.HasOne(e => e.SponsorPlacement)
                  .WithOne(e => e.Game)
                  .HasForeignKey<GameSponsorPlacement>(e => e.GameId)
                  .OnDelete(DeleteBehavior.Cascade);

        });

        modelBuilder.Entity<TournamentRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Kind).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
            entity.HasOne(e => e.Game)
                  .WithMany(e => e.TournamentRegistrations)
                  .HasForeignKey(e => e.GameId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.RegisteredByUser)
                  .WithMany()
                  .HasForeignKey(e => e.RegisteredByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Team)
                  .WithMany()
                  .HasForeignKey(e => e.TeamId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.GameId, e.UserId })
                  .IsUnique()
                  .HasFilter("\"UserId\" IS NOT NULL")
                  .HasDatabaseName("IX_TournamentRegistrations_GameId_UserId_PendingActive");
            entity.HasIndex(e => new { e.GameId, e.TeamId })
                  .IsUnique()
                  .HasFilter("\"TeamId\" IS NOT NULL")
                  .HasDatabaseName("IX_TournamentRegistrations_GameId_TeamId_PendingActive");
            entity.HasIndex(e => new { e.GameId, e.RegisteredByUserId })
                  .HasDatabaseName("IX_TournamentRegistrations_GameId_RegisteredBy_PendingActive");
            entity.HasIndex(e => new { e.GameId, e.Status, e.Kind });
        });

        modelBuilder.Entity<TournamentRegistrationRosterMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfirmationStatus).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
            entity.HasOne(e => e.TournamentRegistration)
                  .WithMany(e => e.RosterMembers)
                  .HasForeignKey(e => e.TournamentRegistrationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Game)
                  .WithMany()
                  .HasForeignKey(e => e.GameId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Team)
                  .WithMany()
                  .HasForeignKey(e => e.TeamId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.GameId, e.UserId })
                  .IsUnique()
                  .HasDatabaseName("IX_TournamentRosterMembers_GameId_UserId_PendingActive");
            entity.HasIndex(e => new { e.GameId, e.TeamId, e.UserId });
            entity.HasIndex(e => new { e.TournamentRegistrationId, e.ConfirmationStatus });
        });

        modelBuilder.Entity<TournamentRosterConfirmationNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.ExpiresAtUtc).IsRequired();
            entity.HasOne(e => e.RosterMember)
                  .WithOne(e => e.ConfirmationNotification)
                  .HasForeignKey<TournamentRosterConfirmationNotification>(e => e.TournamentRegistrationRosterMemberId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Team)
                  .WithMany()
                  .HasForeignKey(e => e.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.TournamentRegistrationRosterMemberId)
                  .IsUnique();
            entity.HasIndex(e => new { e.UserId, e.ExpiresAtUtc });
            entity.HasIndex(e => new { e.TeamId, e.UserId });
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

        modelBuilder.Entity<Placement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Users)
                  .WithMany()
                  .UsingEntity<Dictionary<string, object>>(
                      "PlacementUser",
                      j => j.HasOne<User>()
                          .WithMany()
                          .HasForeignKey("UserId")
                          .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<Placement>()
                          .WithMany()
                          .HasForeignKey("PlacementId")
                          .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasKey("PlacementId", "UserId"));
            entity.HasMany(e => e.Teams)
                  .WithMany()
                  .UsingEntity<Dictionary<string, object>>(
                      "PlacementTeam",
                      j => j.HasOne<Team>()
                          .WithMany()
                          .HasForeignKey("TeamId")
                          .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<Placement>()
                          .WithMany()
                          .HasForeignKey("PlacementId")
                          .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasKey("PlacementId", "TeamId"));
            entity.HasOne(e => e.Game)
                  .WithMany(e => e.Placements)
                  .HasForeignKey(e => e.GameId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sponsor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.LogoUrl).IsRequired();
            entity.Property(e => e.InfoUrl).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1200);
            entity.Property(e => e.SponsorTier)
                  .HasConversion(sponsorTierConverter)
                  .IsRequired();
            entity.HasMany(e => e.GameSponsorPlacements)
                  .WithOne(e => e.Sponsor)
                  .HasForeignKey(e => e.SponsorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GameSponsorPlacement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Context)
                  .HasConversion(sponsorContextConverter)
                  .IsRequired();
            entity.Property(e => e.Headline).HasMaxLength(160);
            entity.Property(e => e.SupportLine).HasMaxLength(220);
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.HasOne(e => e.Sponsor)
                  .WithMany(e => e.GameSponsorPlacements)
                  .HasForeignKey(e => e.SponsorId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.GameId).IsUnique();
        });

        OnModelCreatingPartial(modelBuilder);
    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

}

