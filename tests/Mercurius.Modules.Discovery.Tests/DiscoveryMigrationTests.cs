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
}
