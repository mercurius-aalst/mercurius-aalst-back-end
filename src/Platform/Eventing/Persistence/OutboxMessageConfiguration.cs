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
        entity.Property(message => message.ProcessedAtUtc).HasColumnName("processed_at_utc");
        entity.Property(message => message.LastError).HasColumnName("last_error").HasMaxLength(4000);
        entity.HasIndex(message => new { message.ProcessedAtUtc, message.OccurredAtUtc });
    }
}
