using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class BoundTeamInviteMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE teams.team_invites
                SET "RespondedAt" = "CreatedAt"
                WHERE "Status" IN (1, 2) AND "RespondedAt" IS NULL;

                UPDATE teams.team_invites
                SET "CancelledAt" = "CreatedAt"
                WHERE "Status" = 3 AND "CancelledAt" IS NULL;

                UPDATE teams.team_invites
                SET "ExpiredAt" = "CreatedAt"
                WHERE "Status" = 4 AND "ExpiredAt" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_team_invites_cancelled_retention",
                schema: "teams",
                table: "team_invites",
                columns: new[] { "CancelledAt", "Id" },
                filter: "\"Status\" = 3");

            migrationBuilder.CreateIndex(
                name: "IX_team_invites_expired_retention",
                schema: "teams",
                table: "team_invites",
                columns: new[] { "ExpiredAt", "Id" },
                filter: "\"Status\" = 4");

            migrationBuilder.CreateIndex(
                name: "IX_team_invites_pending_expiration",
                schema: "teams",
                table: "team_invites",
                columns: new[] { "ExpiresAt", "Id" },
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_team_invites_responded_retention",
                schema: "teams",
                table: "team_invites",
                columns: new[] { "RespondedAt", "Id" },
                filter: "\"Status\" = 1 OR \"Status\" = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_team_invites_cancelled_retention",
                schema: "teams",
                table: "team_invites");

            migrationBuilder.DropIndex(
                name: "IX_team_invites_expired_retention",
                schema: "teams",
                table: "team_invites");

            migrationBuilder.DropIndex(
                name: "IX_team_invites_pending_expiration",
                schema: "teams",
                table: "team_invites");

            migrationBuilder.DropIndex(
                name: "IX_team_invites_responded_retention",
                schema: "teams",
                table: "team_invites");
        }
    }
}
