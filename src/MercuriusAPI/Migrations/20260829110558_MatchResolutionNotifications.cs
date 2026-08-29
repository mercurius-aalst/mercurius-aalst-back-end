using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class MatchResolutionNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "match_resolution_notifications",
                schema: "tournament",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientKind = table.Column<int>(type: "integer", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_resolution_notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_match_resolution_notifications_MatchId_CreatedAtUtc",
                schema: "tournament",
                table: "match_resolution_notifications",
                columns: new[] { "MatchId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_match_resolution_notifications_TournamentId_RecipientUserId~",
                schema: "tournament",
                table: "match_resolution_notifications",
                columns: new[] { "TournamentId", "RecipientUserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_resolution_notifications",
                schema: "tournament");
        }
    }
}
