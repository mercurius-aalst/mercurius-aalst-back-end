using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class LeaderboardTournaments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeaderboardRankingMetric",
                schema: "tournament",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LeaderboardRevision",
                schema: "tournament",
                table: "tournaments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "leaderboard_participants",
                schema: "tournament",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leaderboard_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leaderboard_participants_tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalSchema: "tournament",
                        principalTable: "tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leaderboard_attempts",
                schema: "tournament",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leaderboard_attempts", x => x.Id);
                    table.CheckConstraint("CK_leaderboard_attempts_metric_value", "(\"Score\" IS NOT NULL AND \"DurationMilliseconds\" IS NULL AND \"Score\" >= 0) OR (\"Score\" IS NULL AND \"DurationMilliseconds\" IS NOT NULL AND \"DurationMilliseconds\" > 0)");
                    table.ForeignKey(
                        name: "FK_leaderboard_attempts_leaderboard_participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalSchema: "tournament",
                        principalTable: "leaderboard_participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "placement_leaderboard_participants",
                schema: "tournament",
                columns: table => new
                {
                    PlacementId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaderboardParticipantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_placement_leaderboard_participants", x => new { x.PlacementId, x.LeaderboardParticipantId });
                    table.ForeignKey(
                        name: "FK_placement_leaderboard_participants_leaderboard_participants~",
                        column: x => x.LeaderboardParticipantId,
                        principalSchema: "tournament",
                        principalTable: "leaderboard_participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_placement_leaderboard_participants_placements_PlacementId",
                        column: x => x.PlacementId,
                        principalSchema: "tournament",
                        principalTable: "placements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leaderboard_attempts_ParticipantId",
                schema: "tournament",
                table: "leaderboard_attempts",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_leaderboard_participants_TournamentId_LinkedUserId",
                schema: "tournament",
                table: "leaderboard_participants",
                columns: new[] { "TournamentId", "LinkedUserId" },
                unique: true,
                filter: "\"LinkedUserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_placement_leaderboard_participants_LeaderboardParticipantId",
                schema: "tournament",
                table: "placement_leaderboard_participants",
                column: "LeaderboardParticipantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leaderboard_attempts",
                schema: "tournament");

            migrationBuilder.DropTable(
                name: "placement_leaderboard_participants",
                schema: "tournament");

            migrationBuilder.DropTable(
                name: "leaderboard_participants",
                schema: "tournament");

            migrationBuilder.DropColumn(
                name: "LeaderboardRankingMetric",
                schema: "tournament",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "LeaderboardRevision",
                schema: "tournament",
                table: "tournaments");

        }
    }
}
