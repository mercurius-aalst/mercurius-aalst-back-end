using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class UserOwnedTeamManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeamInvites_TeamId",
                table: "TeamInvites");

            migrationBuilder.DropIndex(
                name: "IX_TeamInvites_UserId",
                table: "TeamInvites");

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Teams",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "TeamInvites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAt",
                table: "TeamInvites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "TeamInvites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "TeamInvites"
                SET "ExpiresAt" = "CreatedAt" + INTERVAL '14 days'
                WHERE "ExpiresAt" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "TeamInvites",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.Sql("""
                INSERT INTO "TeamUser" ("TeamId", "UserId")
                SELECT "Id", "CaptainUserId"
                FROM "Teams"
                ON CONFLICT ("TeamId", "UserId") DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_TeamId_Status_ExpiresAt",
                table: "TeamInvites",
                columns: new[] { "TeamId", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_TeamId_UserId_Pending",
                table: "TeamInvites",
                columns: new[] { "TeamId", "UserId" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_UserId_Status_ExpiresAt",
                table: "TeamInvites",
                columns: new[] { "UserId", "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeamInvites_TeamId_Status_ExpiresAt",
                table: "TeamInvites");

            migrationBuilder.DropIndex(
                name: "IX_TeamInvites_TeamId_UserId_Pending",
                table: "TeamInvites");

            migrationBuilder.DropIndex(
                name: "IX_TeamInvites_UserId_Status_ExpiresAt",
                table: "TeamInvites");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "TeamInvites");

            migrationBuilder.DropColumn(
                name: "ExpiredAt",
                table: "TeamInvites");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "TeamInvites");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_TeamId",
                table: "TeamInvites",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_UserId",
                table: "TeamInvites",
                column: "UserId");
        }
    }
}
