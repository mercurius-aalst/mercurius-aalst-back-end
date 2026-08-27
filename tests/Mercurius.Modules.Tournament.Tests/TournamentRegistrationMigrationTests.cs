using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Mercurius.Modules.Tournament.Tests;

public class TournamentRegistrationMigrationTests
{
    [Fact]
    public void TournamentRegistrationSnapshots_IsDiscoveredByEfCore()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseNpgsql("Host=localhost;Database=translation-only")
            .Options;
        using var dbContext = new MercuriusDBContext(options);
        var migrations = dbContext.GetService<IMigrationsAssembly>();

        Assert.Contains("20260729123000_CompetitionRegistrationSnapshots", migrations.Migrations.Keys);
    }

    [Fact]
    public void TournamentRegistrationSnapshots_UsesValidAliasesAndEnforcesRequiredActorSnapshot()
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

    [Fact]
    public void RenameCompetitionGameToTournament_IsReversibleAndPreservesReferences()
    {
        var (sql, reverseSql) = GetRenameMigrationSql();

        Assert.Contains("ALTER SCHEMA competition RENAME TO tournament", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE tournament.games RENAME TO tournaments", sql, StringComparison.Ordinal);
        Assert.Contains("RENAME COLUMN \"GameId\" TO \"TournamentId\"", sql, StringComparison.Ordinal);
        Assert.Contains("SET entity_type = 'tournament'", sql, StringComparison.Ordinal);
        Assert.Contains("platform.outbox_messages", sql, StringComparison.Ordinal);
        Assert.Contains("GameCreatedIntegrationEvent", sql, StringComparison.Ordinal);
        Assert.Contains("MatchCompletedIntegrationEvent", sql, StringComparison.Ordinal);
        Assert.Contains("TournamentRegistrationCreatedIntegrationEvent", sql, StringComparison.Ordinal);
        Assert.Contains("processed_at_utc IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("dead_lettered_at_utc IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("jsonb_build_object('tournamentId'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ALTER SCHEMA tournament RENAME TO competition", reverseSql, StringComparison.Ordinal);
        Assert.Contains("RENAME COLUMN \"TournamentId\" TO \"GameId\"", reverseSql, StringComparison.Ordinal);
        Assert.Contains("SET entity_type = 'game'", reverseSql, StringComparison.Ordinal);
        Assert.Contains("jsonb_build_object('gameId'", reverseSql, StringComparison.Ordinal);
    }

    public static IEnumerable<object[]> LegacyOutboxEventTypeRenames =>
    [
        ["Mercurius.Modules.Competition.Contracts.GameCreatedIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.TournamentCreatedIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.GameUpdatedIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.TournamentUpdatedIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.GameStartedIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.TournamentStartedIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.GameResetIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.TournamentResetIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.GameCompletedIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.TournamentCompletedIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.GameCanceledIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.TournamentCanceledIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.GameDeletedIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.TournamentDeletedIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.MatchCompletedIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.MatchCompletedIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.PlacementAssignedIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.PlacementAssignedIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.RosterMemberConfirmedIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.RosterMemberConfirmedIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.TournamentRegistrationCanceledIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.TournamentRegistrationCanceledIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.TournamentRegistrationCreatedIntegrationEvent", "Mercurius.Modules.Tournament.Contracts.TournamentRegistrationCreatedIntegrationEvent"],
        ["Mercurius.Modules.Competition.Contracts.TournamentRosterConfirmationChangedEvent", "Mercurius.Modules.Tournament.Contracts.TournamentRosterConfirmationChangedEvent"],
        ["Mercurius.Modules.Sponsorship.Contracts.V1.GameSponsorPlacementChanged", "Mercurius.Modules.Sponsorship.Contracts.V1.TournamentSponsorPlacementChanged"]
    ];

    [Theory]
    [MemberData(nameof(LegacyOutboxEventTypeRenames))]
    public void RenameCompetitionGameToTournament_RewritesEveryOutboxEventAliasInBothDirections(
        string legacyType,
        string canonicalType)
    {
        var (sql, reverseSql) = GetRenameMigrationSql();

        Assert.Contains($"event_type LIKE '{legacyType}%'", sql, StringComparison.Ordinal);
        Assert.Contains($"REPLACE(event_type, '{legacyType}', '{canonicalType}')", sql, StringComparison.Ordinal);
        Assert.Contains($"event_type LIKE '{canonicalType}%'", reverseSql, StringComparison.Ordinal);
        Assert.Contains($"REPLACE(event_type, '{canonicalType}', '{legacyType}')", reverseSql, StringComparison.Ordinal);
        Assert.Contains("jsonb_build_object('tournamentId', payload::jsonb->'gameId')", sql, StringComparison.Ordinal);
        Assert.Contains("jsonb_build_object('gameId', payload::jsonb->'tournamentId')", reverseSql, StringComparison.Ordinal);
        Assert.Contains("processed_at_utc IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("dead_lettered_at_utc IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("processed_at_utc IS NULL", reverseSql, StringComparison.Ordinal);
        Assert.Contains("dead_lettered_at_utc IS NULL", reverseSql, StringComparison.Ordinal);
    }

    [Fact]
    public void RenameCompetitionGameToTournament_ContainsNoMisspelledEventNamespace()
    {
        var (sql, reverseSql) = GetRenameMigrationSql();

        Assert.DoesNotContain("Mercuri.Modules", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("Mercuri.Modules", reverseSql, StringComparison.Ordinal);
    }

    private static (string Up, string Down) GetRenameMigrationSql()
    {
        var migration = new RenameCompetitionGameToTournament();
        return (
            string.Join(Environment.NewLine, migration.UpOperations.OfType<SqlOperation>().Select(operation => operation.Sql)),
            string.Join(Environment.NewLine, migration.DownOperations.OfType<SqlOperation>().Select(operation => operation.Sql)));
    }
}
