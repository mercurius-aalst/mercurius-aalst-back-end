using Mercurius.LAN.API.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Mercurius.Modules.Tournament.Tests;

public sealed class MatchResultProvenanceMigrationTests
{
    [Fact]
    public void ProvenanceMigration_BackfillsOnlyUniqueWinnerOrLoserEdges()
    {
        var migration = new MatchResultProvenance();
        var sql = string.Join(
            Environment.NewLine,
            migration.UpOperations.OfType<SqlOperation>().Select(operation => operation.Sql));

        Assert.Contains("WinnerNextMatchId", sql, StringComparison.Ordinal);
        Assert.Contains("LoserNextMatchId", sql, StringComparison.Ordinal);
        Assert.Contains("UserWinnerId", sql, StringComparison.Ordinal);
        Assert.Contains("UserLoserId", sql, StringComparison.Ordinal);
        Assert.Contains("TeamWinnerId", sql, StringComparison.Ordinal);
        Assert.Contains("TeamLoserId", sql, StringComparison.Ordinal);
        Assert.Contains("candidate_count = 1", sql, StringComparison.Ordinal);
        Assert.Contains("ParticipationMode", sql, StringComparison.Ordinal);
        Assert.Contains("TournamentId", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ProvenanceMigration_VersionsRowsChangedByTheBackfill()
    {
        var migration = new MatchResultProvenance();
        var sql = string.Join(
            Environment.NewLine,
            migration.UpOperations.OfType<SqlOperation>().Select(operation => operation.Sql));

        Assert.Contains("SET \"ResultVersion\" = \"ResultVersion\" + 1", sql, StringComparison.Ordinal);
        Assert.Contains("\"Participant1SourceMatchId\" IS NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("\"Participant2SourceMatchId\" IS NOT NULL", sql, StringComparison.Ordinal);
    }
}
