using Mercurius.LAN.API.Models;
using Microsoft.EntityFrameworkCore;
using Mercurius.LAN.API.Models.Auth;

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
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Placement> Placements { get; set; }
    public DbSet<Sponsor> Sponsors { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Username).IsRequired();
            entity.Property(e => e.Firstname).IsRequired();
            entity.Property(e => e.Lastname).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired(false);
            entity.Property(e => e.Salt).IsRequired(false);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(e => e.Name).IsRequired();
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
                   .IsRequired();

        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.Id);
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
            entity.HasMany(e => e.RegisteredUsers)
                  .WithMany()
                  .UsingEntity<Dictionary<string, object>>(
                      "GameUser",
                      j => j.HasOne<User>()
                          .WithMany()
                          .HasForeignKey("UserId")
                          .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<Game>()
                          .WithMany()
                          .HasForeignKey("GameId")
                          .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasKey("GameId", "UserId"));
            entity.HasMany(e => e.RegisteredTeams)
                  .WithMany()
                  .UsingEntity<Dictionary<string, object>>(
                      "GameTeam",
                      j => j.HasOne<Team>()
                          .WithMany()
                          .HasForeignKey("TeamId")
                          .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<Game>()
                          .WithMany()
                          .HasForeignKey("GameId")
                          .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasKey("GameId", "TeamId"));

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
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(e => e.SponsorTier).IsRequired();
        });

        OnModelCreatingPartial(modelBuilder);
    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

}

