using Mercurius.LAN.API.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Mercurius.LAN.API.Tests;

public class DiscoveryMigrationTests
{
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
        Assert.DoesNotContain(operations, operation => operation is DropTableOperation);
        Assert.Contains(operations, operation =>
            operation is SqlOperation sql &&
            sql.Sql.Contains("gin_trgm_ops", StringComparison.Ordinal));
    }
}
