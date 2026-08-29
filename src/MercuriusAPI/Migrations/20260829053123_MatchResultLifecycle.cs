using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class MatchResultLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedAdminUserId",
                schema: "tournament",
                table: "tournaments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CorrectionDeadlineUtc",
                schema: "tournament",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ForfeitedParticipantNumber",
                schema: "tournament",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LifecycleState",
                schema: "tournament",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Participant1CorrectionCount",
                schema: "tournament",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "Participant1EndedConfirmedAtUtc",
                schema: "tournament",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Participant1ReportedScore1",
                schema: "tournament",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Participant1ReportedScore2",
                schema: "tournament",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Participant2CorrectionCount",
                schema: "tournament",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "Participant2EndedConfirmedAtUtc",
                schema: "tournament",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Participant2ReportedScore1",
                schema: "tournament",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Participant2ReportedScore2",
                schema: "tournament",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResultKind",
                schema: "tournament",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultRecordedAtUtc",
                schema: "tournament",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResultRecordedByUserId",
                schema: "tournament",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResultVersion",
                schema: "tournament",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE tournament.matches
                SET "LifecycleState" = 5,
                    "ResultKind" = 0,
                    "ResultRecordedAtUtc" = "EndTime",
                    "ResultVersion" = 1
                WHERE ("UserWinnerId" IS NOT NULL OR "TeamWinnerId" IS NOT NULL)
                  AND ("Participant1Score" IS NOT NULL OR "Participant2Score" IS NOT NULL);
                """);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScoreConfirmationDeadlineUtc",
                schema: "tournament",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedAdminUserId",
                schema: "tournament",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "CorrectionDeadlineUtc",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "ForfeitedParticipantNumber",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "LifecycleState",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Participant1CorrectionCount",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Participant1EndedConfirmedAtUtc",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Participant1ReportedScore1",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Participant1ReportedScore2",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Participant2CorrectionCount",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Participant2EndedConfirmedAtUtc",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Participant2ReportedScore1",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Participant2ReportedScore2",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "ResultKind",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "ResultRecordedAtUtc",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "ResultRecordedByUserId",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "ResultVersion",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "ScoreConfirmationDeadlineUtc",
                schema: "tournament",
                table: "matches");
        }
    }
}
