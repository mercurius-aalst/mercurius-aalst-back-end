using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class TournamentScheduleEstimation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedEndTime",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedStartTime",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AverageGameDurationMinutes",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedEndTime",
                table: "Games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedStartTime",
                table: "Games",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "RoundBreakDurationMinutes",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedEndTime",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "EstimatedStartTime",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AverageGameDurationMinutes",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "EstimatedEndTime",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PlannedStartTime",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "RoundBreakDurationMinutes",
                table: "Games");
        }
    }
}
