using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    public partial class RemovePlayerAndParticipantType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamInvites_Players_PlayerId",
                table: "TeamInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Players_CaptainId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTeam_Players_PlayersId",
                table: "PlayerTeam");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTeam_Teams_TeamsId",
                table: "PlayerTeam");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Participants_Id",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_TeamInvites_PlayerId",
                table: "TeamInvites");

            migrationBuilder.DropIndex(
                name: "IX_Teams_CaptainId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_PlayerTeam_TeamsId",
                table: "PlayerTeam");

            migrationBuilder.RenameColumn(
                name: "ParticipantType",
                table: "Games",
                newName: "ParticipationMode");

            migrationBuilder.RenameColumn(
                name: "ParticipantType",
                table: "Matches",
                newName: "ParticipationMode");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "TeamInvites",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "CaptainId",
                table: "Teams",
                newName: "CaptainUserId");

            migrationBuilder.RenameTable(
                name: "PlayerTeam",
                newName: "TeamUser");

            migrationBuilder.RenameColumn(
                name: "PlayersId",
                table: "TeamUser",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "TeamsId",
                table: "TeamUser",
                newName: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_UserId",
                table: "TeamInvites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CaptainUserId",
                table: "Teams",
                column: "CaptainUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamUser_UserId",
                table: "TeamUser",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamInvites_Users_UserId",
                table: "TeamInvites",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Users_CaptainUserId",
                table: "Teams",
                column: "CaptainUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamUser_Users_UserId",
                table: "TeamUser",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamUser_Teams_TeamId",
                table: "TeamUser",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropTable(
                name: "Players");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    DiscordId = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Firstname = table.Column<string>(type: "text", nullable: false),
                    Lastname = table.Column<string>(type: "text", nullable: false),
                    RiotId = table.Column<string>(type: "text", nullable: true),
                    SteamId = table.Column<string>(type: "text", nullable: true),
                    Username = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Participants_Id",
                        column: x => x.Id,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.DropForeignKey(
                name: "FK_TeamInvites_Users_UserId",
                table: "TeamInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Users_CaptainUserId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamUser_Users_UserId",
                table: "TeamUser");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamUser_Teams_TeamId",
                table: "TeamUser");

            migrationBuilder.DropIndex(
                name: "IX_TeamInvites_UserId",
                table: "TeamInvites");

            migrationBuilder.DropIndex(
                name: "IX_Teams_CaptainUserId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_TeamUser_UserId",
                table: "TeamUser");

            migrationBuilder.RenameColumn(
                name: "ParticipationMode",
                table: "Games",
                newName: "ParticipantType");

            migrationBuilder.RenameColumn(
                name: "ParticipationMode",
                table: "Matches",
                newName: "ParticipantType");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "TeamInvites",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "CaptainUserId",
                table: "Teams",
                newName: "CaptainId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "TeamUser",
                newName: "PlayersId");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "TeamUser",
                newName: "TeamsId");

            migrationBuilder.RenameTable(
                name: "TeamUser",
                newName: "PlayerTeam");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_PlayerId",
                table: "TeamInvites",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CaptainId",
                table: "Teams",
                column: "CaptainId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeam_TeamsId",
                table: "PlayerTeam",
                column: "TeamsId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamInvites_Players_PlayerId",
                table: "TeamInvites",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Players_CaptainId",
                table: "Teams",
                column: "CaptainId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTeam_Players_PlayersId",
                table: "PlayerTeam",
                column: "PlayersId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTeam_Teams_TeamsId",
                table: "PlayerTeam",
                column: "TeamsId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
