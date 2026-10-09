using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Mercurius.Modules.Discovery.Tests;

public class DiscoveryMigrationTests
{
    [Fact]
    public void AddDiscoverySearchProjections_IsDiscoveredByEfCore()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseNpgsql("Host=localhost;Database=translation-only")
            .Options;
        using var dbContext = new MercuriusDBContext(options);

        var migrations = dbContext.GetService<IMigrationsAssembly>();

        Assert.Contains("20260807111110_AddDiscoverySearchProjections", migrations.Migrations.Keys);
    }

    [Fact]
    public void AddDiscoverySearchProjections_CreatesOnlyDiscoveryPersistence()
    {
        var migration = new AddDiscoverySearchProjections();
        var operations = migration.UpOperations.ToList();

        Assert.Contains(operations, operation =>
            operation is EnsureSchemaOperation ensureSchema && ensureSchema.Name == "discovery");
        Assert.Contains(operations, operation =>
            operation is CreateTableOperation table &&
            table.Schema == "discovery" &&
            table.Name == "search_documents");
        Assert.Contains(operations, operation =>
            operation is CreateTableOperation table &&
            table.Schema == "discovery" &&
            table.Name == "search_index_rebuild_jobs");
        Assert.Contains(operations, operation =>
            operation is CreateTableOperation table &&
            table.Schema == "discovery" &&
            table.Name == "search_index_rebuild_documents");
        Assert.DoesNotContain(operations, operation => operation is DropTableOperation);
        Assert.Contains(operations, operation =>
            operation is SqlOperation sql &&
            sql.Sql.Contains("gin_trgm_ops", StringComparison.Ordinal));
        Assert.Contains(operations, operation =>
            operation is SqlOperation sql &&
            sql.Sql.Contains("text_pattern_ops", StringComparison.Ordinal));
    }

    [Fact]
    public void DiscoverySearchQueryIndexOptimization_ReplacesActiveSearchIndexes()
    {
        var migration = new DiscoverySearchQueryIndexOptimization();
        var operations = migration.UpOperations.ToList();

        Assert.Contains(operations, operation =>
            operation is SqlOperation sql &&
            sql.Sql.Contains("DROP INDEX IF EXISTS discovery.\"IX_search_documents_active_exact_order\"", StringComparison.Ordinal) &&
            sql.Sql.Contains("DROP INDEX IF EXISTS discovery.\"IX_search_documents_active_prefix\"", StringComparison.Ordinal) &&
            sql.Sql.Contains("DROP INDEX IF EXISTS discovery.\"IX_search_documents_normalized_text_trgm\"", StringComparison.Ordinal));
        Assert.Contains(operations, operation =>
            operation is AlterColumnOperation column &&
            column.Name == "normalized_text" &&
            column.Collation == "C");
        Assert.Contains(operations, operation =>
            operation is CreateIndexOperation index &&
            index.Name == "IX_search_documents_active_text" &&
            index.Filter == "is_deleted = false");
        Assert.Contains(operations, operation =>
            operation is SqlOperation sql &&
            sql.Sql.Contains("USING gin (normalized_text gin_trgm_ops)", StringComparison.Ordinal) &&
            sql.Sql.Contains("WHERE is_deleted = false", StringComparison.Ordinal) &&
            sql.Sql.Contains("fillfactor = 90", StringComparison.Ordinal) &&
            sql.Sql.Contains("autovacuum_vacuum_scale_factor = 0.05", StringComparison.Ordinal));
    }

    [Fact]
    public void DiscoverySearchQueryIndexOptimization_DownRestoresHistoricalPredicatesAndDefaults()
    {
        var migration = new DiscoverySearchQueryIndexOptimization();
        var operations = migration.DownOperations.ToList();

        Assert.Contains(operations, operation =>
            operation is AlterColumnOperation column &&
            column.Name == "normalized_text" &&
            column.OldColumn?.Collation == "C" &&
            column.Collation is null);
        Assert.Contains(operations, operation =>
            operation is CreateIndexOperation index &&
            index.Name == "IX_search_documents_active_exact_order" &&
            index.Filter == "is_deleted = false AND entity_type IN ('user', 'team', 'tournament')");
        Assert.Contains(operations, operation =>
            operation is SqlOperation sql &&
            sql.Sql.Contains("RESET (fillfactor, autovacuum_vacuum_scale_factor)", StringComparison.Ordinal) &&
            sql.Sql.Contains("text_pattern_ops", StringComparison.Ordinal) &&
            sql.Sql.Contains("WHERE is_deleted = false AND entity_type IN ('user', 'team', 'game')", StringComparison.Ordinal) &&
            sql.Sql.Contains("USING gin (normalized_text gin_trgm_ops)", StringComparison.Ordinal));
    }
}
