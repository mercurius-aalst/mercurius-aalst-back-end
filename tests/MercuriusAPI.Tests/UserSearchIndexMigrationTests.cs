using System.Reflection;
using Mercurius.LAN.API.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Mercurius.LAN.API.Tests;

public class UserSearchIndexMigrationTests
{
    [Fact]
    public void AuthenticatedUserSearchIndexes_CreatesRequiredIndexes()
    {
        var operations = GetMigrationOperations("Up");
        var sql = string.Join("\n", operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        Assert.Contains("CREATE EXTENSION IF NOT EXISTS pg_trgm", sql);
        Assert.Contains("CREATE INDEX \"IX_Users_AuthenticatedSearch_NormalizedUsername_Trgm\"", sql);
        Assert.Contains("ON \"Users\" USING gin (\"NormalizedUsername\" gin_trgm_ops)", sql);
        Assert.Contains("CREATE INDEX \"IX_Users_AuthenticatedSearch_Cursor\"", sql);
        Assert.Contains("ON \"Users\" (\"NormalizedUsername\", \"Id\")", sql);
        Assert.Contains("AND \"IsDeleted\" = false", sql);
        Assert.Contains("\"Username\" <> ''", sql);
        Assert.Contains("\"NormalizedUsername\" <> ''", sql);
        Assert.DoesNotContain("\"Firstname\"", sql);
        Assert.DoesNotContain("\"Lastname\"", sql);
    }

    [Fact]
    public void AuthenticatedUserSearchIndexes_DropsRequiredIndexes()
    {
        var operations = GetMigrationOperations("Down");
        var sql = string.Join("\n", operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        Assert.Contains("DROP INDEX \"IX_Users_AuthenticatedSearch_Cursor\"", sql);
        Assert.Contains("DROP INDEX \"IX_Users_AuthenticatedSearch_NormalizedUsername_Trgm\"", sql);
    }

    private static IReadOnlyList<MigrationOperation> GetMigrationOperations(string methodName)
    {
        var migration = new AuthenticatedUserSearchIndexes();
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        typeof(AuthenticatedUserSearchIndexes)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(migration, [migrationBuilder]);

        return migrationBuilder.Operations;
    }
}
