using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Platform.Eventing.Persistence;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> entity)
    {
        entity.ToTable("outbox_messages", "platform");
        entity.HasKey(message => message.Id);
        entity.Property(message => message.Id).HasColumnName("id");
        entity.Property(message => message.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(300);
        entity.Property(message => message.Payload).HasColumnName("payload").IsRequired();
        entity.Property(message => message.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        entity.Property(message => message.RetryCount).HasColumnName("retry_count").IsRequired();
        entity.Property(message => message.LastAttemptAtUtc).HasColumnName("last_attempt_at_utc");
        entity.Property(message => message.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc");
        entity.Property(message => message.LeaseId).HasColumnName("lease_id");
        entity.Property(message => message.LeaseExpiresAtUtc).HasColumnName("lease_expires_at_utc");
        entity.Property(message => message.ProcessedAtUtc).HasColumnName("processed_at_utc");
        entity.Property(message => message.DeadLetteredAtUtc).HasColumnName("dead_lettered_at_utc");
        entity.Property(message => message.LastError).HasColumnName("last_error").HasMaxLength(4000);
        entity.HasIndex(message => new
        {
            message.NextAttemptAtUtc,
            message.LeaseExpiresAtUtc,
            message.OccurredAtUtc,
            message.Id
        })
            .HasDatabaseName("IX_outbox_messages_pending_claim")
            .HasFilter("processed_at_utc IS NULL AND dead_lettered_at_utc IS NULL");
        entity.HasIndex(message => new { message.ProcessedAtUtc, message.Id })
            .HasDatabaseName("IX_outbox_messages_processed_retention")
            .HasFilter("processed_at_utc IS NOT NULL");
        entity.HasIndex(message => new { message.DeadLetteredAtUtc, message.Id })
            .HasDatabaseName("IX_outbox_messages_dead_letter_retention")
            .HasFilter("dead_lettered_at_utc IS NOT NULL");
    }
}
