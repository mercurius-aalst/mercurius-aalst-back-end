using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class MatchResultProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Participant1SourceMatchId",
                schema: "tournament",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Participant2SourceMatchId",
                schema: "tournament",
                table: "matches",
                type: "uuid",
                nullable: true);

            // Legacy bracket advancement stored the propagated participant but not
            // the source edge. Backfill only when the incoming winner/loser link
            // and participant identity identify exactly one source. Ambiguous or
            // inconsistent rows remain unprovenanced and are rejected by the
            // reversal guard instead of being cleared optimistically.
            migrationBuilder.Sql("""
                WITH candidates AS (
                    SELECT downstream."Id" AS downstream_id, source."Id" AS source_id
                    FROM tournament.matches AS source
                    INNER JOIN tournament.matches AS downstream
                        ON downstream."Id" = source."WinnerNextMatchId"
                       AND downstream."TournamentId" = source."TournamentId"
                       AND downstream."ParticipationMode" = source."ParticipationMode"
                    WHERE downstream."Participant1SourceMatchId" IS NULL
                      AND (
                          downstream."UserParticipant1Id" = source."UserWinnerId"
                          OR downstream."TeamParticipant1Id" = source."TeamWinnerId"
                      )
                    UNION ALL
                    SELECT downstream."Id", source."Id"
                    FROM tournament.matches AS source
                    INNER JOIN tournament.matches AS downstream
                        ON downstream."Id" = source."LoserNextMatchId"
                       AND downstream."TournamentId" = source."TournamentId"
                       AND downstream."ParticipationMode" = source."ParticipationMode"
                    WHERE downstream."Participant1SourceMatchId" IS NULL
                      AND (
                          downstream."UserParticipant1Id" = source."UserLoserId"
                          OR downstream."TeamParticipant1Id" = source."TeamLoserId"
                      )
                ), unique_candidates AS (
                    SELECT downstream_id, source_id
                    FROM (
                        SELECT candidates.*,
                               COUNT(*) OVER (PARTITION BY downstream_id) AS candidate_count
                        FROM candidates
                    ) AS counted
                    WHERE candidate_count = 1
                )
                UPDATE tournament.matches AS downstream
                SET "Participant1SourceMatchId" = unique_candidates.source_id
                FROM unique_candidates
                WHERE downstream."Id" = unique_candidates.downstream_id;
                """);

            migrationBuilder.Sql("""
                WITH candidates AS (
                    SELECT downstream."Id" AS downstream_id, source."Id" AS source_id
                    FROM tournament.matches AS source
                    INNER JOIN tournament.matches AS downstream
                        ON downstream."Id" = source."WinnerNextMatchId"
                       AND downstream."TournamentId" = source."TournamentId"
                       AND downstream."ParticipationMode" = source."ParticipationMode"
                    WHERE downstream."Participant2SourceMatchId" IS NULL
                      AND (
                          downstream."UserParticipant2Id" = source."UserWinnerId"
                          OR downstream."TeamParticipant2Id" = source."TeamWinnerId"
                      )
                    UNION ALL
                    SELECT downstream."Id", source."Id"
                    FROM tournament.matches AS source
                    INNER JOIN tournament.matches AS downstream
                        ON downstream."Id" = source."LoserNextMatchId"
                       AND downstream."TournamentId" = source."TournamentId"
                       AND downstream."ParticipationMode" = source."ParticipationMode"
                    WHERE downstream."Participant2SourceMatchId" IS NULL
                      AND (
                          downstream."UserParticipant2Id" = source."UserLoserId"
                          OR downstream."TeamParticipant2Id" = source."TeamLoserId"
                      )
                ), unique_candidates AS (
                    SELECT downstream_id, source_id
                    FROM (
                        SELECT candidates.*,
                               COUNT(*) OVER (PARTITION BY downstream_id) AS candidate_count
                        FROM candidates
                    ) AS counted
                    WHERE candidate_count = 1
                )
                UPDATE tournament.matches AS downstream
                SET "Participant2SourceMatchId" = unique_candidates.source_id
                FROM unique_candidates
                WHERE downstream."Id" = unique_candidates.downstream_id;
                """);

            migrationBuilder.Sql("""
                UPDATE tournament.matches
                SET "ResultVersion" = "ResultVersion" + 1
                WHERE "Participant1SourceMatchId" IS NOT NULL
                   OR "Participant2SourceMatchId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Participant1SourceMatchId",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Participant2SourceMatchId",
                schema: "tournament",
                table: "matches");
        }
    }
}
