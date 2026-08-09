using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Mercurius.Modules.Competition.Tests;

public class CompetitionRegistrationMigrationTests
{
    [Fact]
    public void CompetitionRegistrationSnapshots_IsDiscoveredByEfCore()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseNpgsql("Host=localhost;Database=translation-only")
            .Options;
        using var dbContext = new MercuriusDBContext(options);
        var migrations = dbContext.GetService<IMigrationsAssembly>();

        Assert.Contains("20260729123000_CompetitionRegistrationSnapshots", migrations.Migrations.Keys);
    }

    [Fact]
    public void CompetitionRegistrationSnapshots_UsesValidAliasesAndEnforcesRequiredActorSnapshot()
    {
        var migration = new CompetitionRegistrationSnapshots();
        var sql = string.Join(
            Environment.NewLine,
            migration.UpOperations.OfType<SqlOperation>().Select(operation => operation.Sql));

        Assert.Contains("AS user_profile", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("current_user.", sql, StringComparison.Ordinal);
        Assert.Contains(
            "ALTER COLUMN \"RegisteredByUsernameAtRegistration\" SET NOT NULL",
            sql,
            StringComparison.Ordinal);
    }
}
