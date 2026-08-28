using Mercurius.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Discovery.Infrastructure;

internal sealed class SearchDocumentConfiguration : IEntityTypeConfiguration<SearchDocument>
{
    public void Configure(EntityTypeBuilder<SearchDocument> entity)
    {
        entity.ToTable("search_documents", "discovery");
        entity.HasKey(document => document.Id);
        entity.Property(document => document.Id).HasColumnName("id");
        entity.Property(document => document.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
        entity.Property(document => document.EntityId).HasColumnName("entity_id").HasMaxLength(100).IsRequired();
        entity.Property(document => document.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        entity.Property(document => document.Subtitle).HasColumnName("subtitle").HasMaxLength(500).IsRequired();
        entity.Property(document => document.ImageUrl).HasColumnName("image_url").HasMaxLength(2048);
        entity.Property(document => document.Route).HasColumnName("route").HasMaxLength(2048).IsRequired();
        entity.Property(document => document.NormalizedText).HasColumnName("normalized_text").HasMaxLength(1000).IsRequired();
        entity.Property(document => document.TypeOrder).HasColumnName("type_order").IsRequired();
        entity.Property(document => document.SourceVersion).HasColumnName("source_version").IsRequired();
        entity.Property(document => document.IsDeleted).HasColumnName("is_deleted").IsRequired();
        entity.Property(document => document.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        entity.HasIndex(document => new { document.EntityType, document.EntityId }).IsUnique();
        entity.HasIndex(document => new { document.NormalizedText, document.TypeOrder, document.EntityId })
            .HasDatabaseName("IX_search_documents_active_exact_order")
            .HasFilter("is_deleted = false AND entity_type IN ('user', 'team', 'tournament')");
    }
}
