using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Platform.Eventing.Persistence;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> entity)
    {
        entity.ToTable("inbox_messages", "platform");
        entity.HasKey(message => new { message.ConsumerName, message.MessageId });
        entity.Property(message => message.ConsumerName).HasColumnName("consumer_name").HasMaxLength(200).IsRequired();
        entity.Property(message => message.MessageId).HasColumnName("message_id").IsRequired();
        entity.Property(message => message.ProcessedAtUtc).HasColumnName("processed_at_utc").IsRequired();
        entity.HasIndex(message => message.MessageId)
            .HasDatabaseName("IX_inbox_messages_message_id");
    }
}
