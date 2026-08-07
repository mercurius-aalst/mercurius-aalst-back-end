using Mercurius.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercurius.Modules.Discovery.Infrastructure;

internal sealed class SearchIndexRebuildJobConfiguration : IEntityTypeConfiguration<SearchIndexRebuildJob>
{
    public void Configure(EntityTypeBuilder<SearchIndexRebuildJob> entity)
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
    }
}
