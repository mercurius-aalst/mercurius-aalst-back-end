using MercuriusAPI.Models.LAN;
using Microsoft.EntityFrameworkCore;
using MercuriusAPI.Models;
using MercuriusAPI.Models.Auth;

namespace MercuriusAPI.Data
{
    public partial class MercuriusDBContext : DbContext
    {
        public MercuriusDBContext()
        {
        }

        public MercuriusDBContext(DbContextOptions<MercuriusDBContext> options) : base(options)
        {
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<TeamInvite> TeamInvites { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>(entity =>
            {
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.Firstname).IsRequired();
                entity.Property(e => e.Lastname).IsRequired();
                entity.Property(e => e.Email).IsRequired();

            });

            modelBuilder.Entity<Participant>().UseTptMappingStrategy();
            ;
            modelBuilder.Entity<Team>(entity =>
            {
                entity.Property(e => e.Name).IsRequired();
                entity.HasMany(e => e.Players)
                      .WithMany(e => e.Teams);
                entity.HasOne(e => e.Captain)
                       .WithMany()
                       .HasForeignKey(e => e.CaptainId)
                       .IsRequired();

            });

            modelBuilder.Entity<Match>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Participant1)
                      .WithMany()
                      .HasForeignKey(e => e.Participant1Id).IsRequired(false);
                entity.HasOne(e => e.Participant2)
                      .WithMany()
                      .HasForeignKey(e => e.Participant2Id).IsRequired(false);
                entity.HasOne(e => e.Winner)
                        .WithMany()
                        .HasForeignKey(e => e.WinnerId).IsRequired(false);
                entity.HasOne(e => e.Game)
                        .WithMany(e => e.Matches)
                        .HasForeignKey(e => e.GameId).OnDelete(DeleteBehavior.Cascade);      
                entity.HasOne(e => e.Loser)
                        .WithMany()
                        .HasForeignKey(e => e.LoserId).IsRequired(false);
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
                entity.HasMany(e => e.Participants)
                      .WithMany(e => e.Games);
            });

            modelBuilder.Entity<TeamInvite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Team)
                      .WithMany(t => t.TeamInvites)
                      .HasForeignKey(e => e.TeamId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Player)
                      .WithMany()
                      .HasForeignKey(e => e.PlayerId)
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

            OnModelCreatingPartial(modelBuilder);
        }
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    }
}
