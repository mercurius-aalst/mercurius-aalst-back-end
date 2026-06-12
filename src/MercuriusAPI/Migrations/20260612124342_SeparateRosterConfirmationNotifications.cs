using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class SeparateRosterConfirmationNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TournamentRegistrationRosterMembers_TeamInvites_Confirmatio~",
                table: "TournamentRegistrationRosterMembers");

            migrationBuilder.DropIndex(
                name: "IX_TournamentRegistrationRosterMembers_ConfirmationInviteId",
                table: "TournamentRegistrationRosterMembers");

            migrationBuilder.DropIndex(
                name: "IX_TeamInvites_TeamId_UserId_Pending",
                table: "TeamInvites");

            migrationBuilder.DropIndex(
                name: "IX_TeamInvites_TournamentRegistrationRosterMemberId_Purpose",
                table: "TeamInvites");

            migrationBuilder.DropColumn(
                name: "ConfirmationInviteId",
                table: "TournamentRegistrationRosterMembers");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "TeamInvites");

            migrationBuilder.DropColumn(
                name: "TournamentRegistrationRosterMemberId",
                table: "TeamInvites");

            migrationBuilder.CreateTable(
                name: "TournamentRosterConfirmationNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentRegistrationRosterMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRosterConfirmationNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentRosterConfirmationNotifications_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRosterConfirmationNotifications_TournamentRegistr~",
                        column: x => x.TournamentRegistrationRosterMemberId,
                        principalTable: "TournamentRegistrationRosterMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRosterConfirmationNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_TeamId_UserId_Pending",
                table: "TeamInvites",
                columns: new[] { "TeamId", "UserId" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRosterConfirmationNotifications_TeamId_UserId",
                table: "TournamentRosterConfirmationNotifications",
                columns: new[] { "TeamId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRosterConfirmationNotifications_TournamentRegistr~",
                table: "TournamentRosterConfirmationNotifications",
                column: "TournamentRegistrationRosterMemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRosterConfirmationNotifications_UserId_ExpiresAtU~",
                table: "TournamentRosterConfirmationNotifications",
                columns: new[] { "UserId", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentRosterConfirmationNotifications");

            migrationBuilder.DropIndex(
                name: "IX_TeamInvites_TeamId_UserId_Pending",
                table: "TeamInvites");

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmationInviteId",
                table: "TournamentRegistrationRosterMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                table: "TeamInvites",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TournamentRegistrationRosterMemberId",
                table: "TeamInvites",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationRosterMembers_ConfirmationInviteId",
                table: "TournamentRegistrationRosterMembers",
                column: "ConfirmationInviteId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_TeamId_UserId_Pending",
                table: "TeamInvites",
                columns: new[] { "TeamId", "UserId" },
                unique: true,
                filter: "\"Status\" = 0 AND \"Purpose\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_TournamentRegistrationRosterMemberId_Purpose",
                table: "TeamInvites",
                columns: new[] { "TournamentRegistrationRosterMemberId", "Purpose" });

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentRegistrationRosterMembers_TeamInvites_Confirmatio~",
                table: "TournamentRegistrationRosterMembers",
                column: "ConfirmationInviteId",
                principalTable: "TeamInvites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
