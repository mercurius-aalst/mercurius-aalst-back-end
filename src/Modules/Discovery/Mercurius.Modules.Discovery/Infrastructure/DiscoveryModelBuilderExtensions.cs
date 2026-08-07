using Mercurius.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Discovery.Infrastructure;

internal static class DiscoveryModelBuilderExtensions
{
    public static ModelBuilder ApplyDiscoveryConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SearchDocument>(entity =>
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
            entity.Property(document => document.SourceVersion).HasColumnName("source_version").IsRequired();
            entity.Property(document => document.IsDeleted).HasColumnName("is_deleted").IsRequired();
            entity.Property(document => document.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.HasIndex(document => new { document.EntityType, document.EntityId }).IsUnique();
            entity.HasIndex(document => new { document.IsDeleted, document.EntityType, document.Title, document.EntityId });
        });

        modelBuilder.Entity<SearchIndexRebuildJob>(entity =>
        {
            entity.ToTable("search_index_rebuild_jobs", "discovery");
            entity.HasKey(job => job.Id);
            entity.Property(job => job.Id).HasColumnName("id");
            entity.Property(job => job.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(job => job.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(job => job.StartedAtUtc).HasColumnName("started_at_utc");
            entity.Property(job => job.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(job => job.Error).HasColumnName("error").HasMaxLength(4000);
            entity.HasIndex(job => new { job.Status, job.CreatedAtUtc });
        });

        return modelBuilder;
    }
}
