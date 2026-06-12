using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class TeamRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameTeam");

            migrationBuilder.DropTable(
                name: "GameUser");

            migrationBuilder.DropColumn(
                name: "RegisterFormUrl",
                table: "Games");

            migrationBuilder.AddColumn<int>(
                name: "TeamSize",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TournamentRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RegisteredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrations_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrations_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrations_Users_RegisteredByUserId",
                        column: x => x.RegisteredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentRegistrationRosterMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsCaptain = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmationStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRegistrationRosterMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrationRosterMembers_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrationRosterMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrationRosterMembers_TournamentRegistrations~",
                        column: x => x.TournamentRegistrationId,
                        principalTable: "TournamentRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrationRosterMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });



            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationRosterMembers_GameId_TeamId_UserId",
                table: "TournamentRegistrationRosterMembers",
                columns: new[] { "GameId", "TeamId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationRosterMembers_TeamId",
                table: "TournamentRegistrationRosterMembers",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationRosterMembers_TournamentRegistrationI~",
                table: "TournamentRegistrationRosterMembers",
                columns: new[] { "TournamentRegistrationId", "ConfirmationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationRosterMembers_UserId",
                table: "TournamentRegistrationRosterMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRosterMembers_GameId_UserId_PendingActive",
                table: "TournamentRegistrationRosterMembers",
                columns: new[] { "GameId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_GameId_RegisteredBy_PendingActive",
                table: "TournamentRegistrations",
                columns: new[] { "GameId", "RegisteredByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_GameId_Status_Kind",
                table: "TournamentRegistrations",
                columns: new[] { "GameId", "Status", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_GameId_TeamId_PendingActive",
                table: "TournamentRegistrations",
                columns: new[] { "GameId", "TeamId" },
                unique: true,
                filter: "\"TeamId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_GameId_UserId_PendingActive",
                table: "TournamentRegistrations",
                columns: new[] { "GameId", "UserId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_RegisteredByUserId",
                table: "TournamentRegistrations",
                column: "RegisteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_TeamId",
                table: "TournamentRegistrations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_UserId",
                table: "TournamentRegistrations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentRegistrationRosterMembers");

            migrationBuilder.DropTable(
                name: "TournamentRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TeamSize",
                table: "Games");

            migrationBuilder.AddColumn<string>(
                name: "RegisterFormUrl",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "GameTeam",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameTeam", x => new { x.GameId, x.TeamId });
                    table.ForeignKey(
                        name: "FK_GameTeam_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameTeam_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameUser",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameUser", x => new { x.GameId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GameUser_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameUser_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameTeam_TeamId",
                table: "GameTeam",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_GameUser_UserId",
                table: "GameUser",
                column: "UserId");
        }
    }
}
