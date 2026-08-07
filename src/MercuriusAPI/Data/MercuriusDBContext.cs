using Mercurius.Modules.Competition;
using Mercurius.Modules.Discovery;
using Mercurius.Modules.Identity.Infrastructure;
using Mercurius.Modules.Sponsorship;
using Mercurius.Modules.Teams;
using Mercurius.Modules.Teams.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Platform.Eventing.Persistence;

namespace Mercurius.LAN.API.Data;

public partial class MercuriusDBContext : DbContext, IModuleEventDbContext, IIdentityDbContext
{
    public MercuriusDBContext()
    {
    }

    public MercuriusDBContext(DbContextOptions<MercuriusDBContext> options) : base(options)
    {
    }

    public DbSet<Team> Teams { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Auth0UserId).IsUnique();
            entity.HasIndex(user => user.Username)
                .IsUnique()
                .HasFilter("\"Username\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.HasIndex(user => user.NormalizedUsername)
                .IsUnique()
                .HasFilter("\"NormalizedUsername\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.HasIndex(user => user.Email)
                .IsUnique()
                .HasFilter("\"Email\" IS NOT NULL AND \"IsDeleted\" = false");
            entity.Property(user => user.Auth0UserId).IsRequired().HasMaxLength(200);
            entity.Property(user => user.Username).HasMaxLength(32);
            entity.Property(user => user.NormalizedUsername).HasMaxLength(32);
            entity.Property(user => user.Firstname).HasMaxLength(100);
            entity.Property(user => user.Lastname).HasMaxLength(100);
            entity.Property(user => user.Email).HasMaxLength(254);
            entity.Property(user => user.DiscordId).HasMaxLength(100);
            entity.Property(user => user.SteamId).HasMaxLength(100);
            entity.Property(user => user.RiotId).HasMaxLength(100);
            entity.Property(user => user.CreatedAtUtc).IsRequired();
            entity.Property(user => user.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.ApplyTeamsModelConfiguration();
        modelBuilder.ApplyCompetitionModelConfiguration<User, Team>();
        modelBuilder.ApplySponsorshipModelConfiguration();
        modelBuilder.ApplyDiscoveryModelConfiguration();
        ConfigureGameSponsorPlacementRelationship(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages", "platform");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id).HasColumnName("id");
            entity.Property(message => message.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(300);
            entity.Property(message => message.Payload).HasColumnName("payload").IsRequired();
            entity.Property(message => message.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
            entity.Property(message => message.RetryCount).HasColumnName("retry_count").IsRequired();
            entity.Property(message => message.LastAttemptAtUtc).HasColumnName("last_attempt_at_utc");
            entity.Property(message => message.ProcessedAtUtc).HasColumnName("processed_at_utc");
            entity.Property(message => message.LastError).HasColumnName("last_error").HasMaxLength(4000);
            entity.HasIndex(message => new { message.ProcessedAtUtc, message.OccurredAtUtc });
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("inbox_messages", "platform");
            entity.HasKey(message => new { message.ConsumerName, message.MessageId });
            entity.Property(message => message.ConsumerName).HasColumnName("consumer_name").HasMaxLength(200).IsRequired();
            entity.Property(message => message.MessageId).HasColumnName("message_id").IsRequired();
            entity.Property(message => message.ProcessedAtUtc).HasColumnName("processed_at_utc").IsRequired();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    private static void ConfigureGameSponsorPlacementRelationship(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity("Mercurius.Modules.Competition.Domain.Game")
            .HasOne("Mercurius.Modules.Sponsorship.Domain.GameSponsorPlacement", null)
            .WithOne()
            .HasForeignKey("Mercurius.Modules.Sponsorship.Domain.GameSponsorPlacement", "GameId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
