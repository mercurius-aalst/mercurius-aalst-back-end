using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    // Reset-only migration: existing databases are expected to be recreated instead of upgraded in place.
    /// <inheritdoc />
    public partial class RemovePlayerAndParticipantType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Participants_LoserId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Participants_Participant1Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Participants_Participant2Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Participants_WinnerId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamInvites_Players_PlayerId",
                table: "TeamInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Participants_Id",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Players_CaptainId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "GameParticipant");

            migrationBuilder.DropTable(
                name: "ParticipantPlacement");

            migrationBuilder.DropTable(
                name: "PlayerTeam");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.RenameColumn(
                name: "CaptainId",
                table: "Teams",
                newName: "CaptainUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Teams_CaptainId",
                table: "Teams",
                newName: "IX_Teams_CaptainUserId");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "TeamInvites",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TeamInvites_PlayerId",
                table: "TeamInvites",
                newName: "IX_TeamInvites_UserId");

            migrationBuilder.RenameColumn(
                name: "WinnerId",
                table: "Matches",
                newName: "UserWinnerId");

            migrationBuilder.RenameColumn(
                name: "ParticipantType",
                table: "Matches",
                newName: "ParticipationMode");

            migrationBuilder.RenameColumn(
                name: "Participant2Id",
                table: "Matches",
                newName: "UserParticipant2Id");

            migrationBuilder.RenameColumn(
                name: "Participant1Id",
                table: "Matches",
                newName: "UserParticipant1Id");

            migrationBuilder.RenameColumn(
                name: "LoserId",
                table: "Matches",
                newName: "UserLoserId");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_WinnerId",
                table: "Matches",
                newName: "IX_Matches_UserWinnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_Participant2Id",
                table: "Matches",
                newName: "IX_Matches_UserParticipant2Id");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_Participant1Id",
                table: "Matches",
                newName: "IX_Matches_UserParticipant1Id");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_LoserId",
                table: "Matches",
                newName: "IX_Matches_UserLoserId");

            migrationBuilder.RenameColumn(
                name: "ParticipantType",
                table: "Games",
                newName: "ParticipationMode");

            migrationBuilder.AddColumn<string>(
                name: "DiscordId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Firstname",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Lastname",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RiotId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SteamId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Teams",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "TeamLoserId",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamParticipant1Id",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamParticipant2Id",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamWinnerId",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GameTeam",
                columns: table => new
                {
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false)
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
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "PlacementTeam",
                columns: table => new
                {
                    PlacementId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacementTeam", x => new { x.PlacementId, x.TeamId });
                    table.ForeignKey(
                        name: "FK_PlacementTeam_Placements_PlacementId",
                        column: x => x.PlacementId,
                        principalTable: "Placements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlacementTeam_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlacementUser",
                columns: table => new
                {
                    PlacementId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacementUser", x => new { x.PlacementId, x.UserId });
                    table.ForeignKey(
                        name: "FK_PlacementUser_Placements_PlacementId",
                        column: x => x.PlacementId,
                        principalTable: "Placements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlacementUser_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamUser",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamUser", x => new { x.TeamId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TeamUser_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamUser_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamLoserId",
                table: "Matches",
                column: "TeamLoserId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamParticipant1Id",
                table: "Matches",
                column: "TeamParticipant1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamParticipant2Id",
                table: "Matches",
                column: "TeamParticipant2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamWinnerId",
                table: "Matches",
                column: "TeamWinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameTeam_TeamId",
                table: "GameTeam",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_GameUser_UserId",
                table: "GameUser",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlacementTeam_TeamId",
                table: "PlacementTeam",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlacementUser_UserId",
                table: "PlacementUser",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamUser_UserId",
                table: "TeamUser",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamLoserId",
                table: "Matches",
                column: "TeamLoserId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamParticipant1Id",
                table: "Matches",
                column: "TeamParticipant1Id",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamParticipant2Id",
                table: "Matches",
                column: "TeamParticipant2Id",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamWinnerId",
                table: "Matches",
                column: "TeamWinnerId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_UserLoserId",
                table: "Matches",
                column: "UserLoserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_UserParticipant1Id",
                table: "Matches",
                column: "UserParticipant1Id",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_UserParticipant2Id",
                table: "Matches",
                column: "UserParticipant2Id",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_UserWinnerId",
                table: "Matches",
                column: "UserWinnerId",
                principalTable: "Users",
                principalColumn: "Id");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamLoserId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamParticipant1Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamParticipant2Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamWinnerId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_UserLoserId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_UserParticipant1Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_UserParticipant2Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_UserWinnerId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamInvites_Users_UserId",
                table: "TeamInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Users_CaptainUserId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "GameTeam");

            migrationBuilder.DropTable(
                name: "GameUser");

            migrationBuilder.DropTable(
                name: "PlacementTeam");

            migrationBuilder.DropTable(
                name: "PlacementUser");

            migrationBuilder.DropTable(
                name: "TeamUser");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TeamLoserId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TeamParticipant1Id",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TeamParticipant2Id",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TeamWinnerId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "DiscordId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Firstname",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Lastname",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RiotId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SteamId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TeamLoserId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamParticipant1Id",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamParticipant2Id",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamWinnerId",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "CaptainUserId",
                table: "Teams",
                newName: "CaptainId");

            migrationBuilder.RenameIndex(
                name: "IX_Teams_CaptainUserId",
                table: "Teams",
                newName: "IX_Teams_CaptainId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "TeamInvites",
                newName: "PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_TeamInvites_UserId",
                table: "TeamInvites",
                newName: "IX_TeamInvites_PlayerId");

            migrationBuilder.RenameColumn(
                name: "UserWinnerId",
                table: "Matches",
                newName: "WinnerId");

            migrationBuilder.RenameColumn(
                name: "UserParticipant2Id",
                table: "Matches",
                newName: "Participant2Id");

            migrationBuilder.RenameColumn(
                name: "UserParticipant1Id",
                table: "Matches",
                newName: "Participant1Id");

            migrationBuilder.RenameColumn(
                name: "UserLoserId",
                table: "Matches",
                newName: "LoserId");

            migrationBuilder.RenameColumn(
                name: "ParticipationMode",
                table: "Matches",
                newName: "ParticipantType");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_UserWinnerId",
                table: "Matches",
                newName: "IX_Matches_WinnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_UserParticipant2Id",
                table: "Matches",
                newName: "IX_Matches_Participant2Id");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_UserParticipant1Id",
                table: "Matches",
                newName: "IX_Matches_Participant1Id");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_UserLoserId",
                table: "Matches",
                newName: "IX_Matches_LoserId");

            migrationBuilder.RenameColumn(
                name: "ParticipationMode",
                table: "Games",
                newName: "ParticipantType");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Teams",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameParticipant",
                columns: table => new
                {
                    GamesId = table.Column<int>(type: "integer", nullable: false),
                    ParticipantsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameParticipant", x => new { x.GamesId, x.ParticipantsId });
                    table.ForeignKey(
                        name: "FK_GameParticipant_Games_GamesId",
                        column: x => x.GamesId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameParticipant_Participants_ParticipantsId",
                        column: x => x.ParticipantsId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantPlacement",
                columns: table => new
                {
                    ParticipantsId = table.Column<int>(type: "integer", nullable: false),
                    PlacementId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantPlacement", x => new { x.ParticipantsId, x.PlacementId });
                    table.ForeignKey(
                        name: "FK_ParticipantPlacement_Participants_ParticipantsId",
                        column: x => x.ParticipantsId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipantPlacement_Placements_PlacementId",
                        column: x => x.PlacementId,
                        principalTable: "Placements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "PlayerTeam",
                columns: table => new
                {
                    PlayersId = table.Column<int>(type: "integer", nullable: false),
                    TeamsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTeam", x => new { x.PlayersId, x.TeamsId });
                    table.ForeignKey(
                        name: "FK_PlayerTeam_Players_PlayersId",
                        column: x => x.PlayersId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTeam_Teams_TeamsId",
                        column: x => x.TeamsId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameParticipant_ParticipantsId",
                table: "GameParticipant",
                column: "ParticipantsId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantPlacement_PlacementId",
                table: "ParticipantPlacement",
                column: "PlacementId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeam_TeamsId",
                table: "PlayerTeam",
                column: "TeamsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Participants_LoserId",
                table: "Matches",
                column: "LoserId",
                principalTable: "Participants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Participants_Participant1Id",
                table: "Matches",
                column: "Participant1Id",
                principalTable: "Participants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Participants_Participant2Id",
                table: "Matches",
                column: "Participant2Id",
                principalTable: "Participants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Participants_WinnerId",
                table: "Matches",
                column: "WinnerId",
                principalTable: "Participants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamInvites_Players_PlayerId",
                table: "TeamInvites",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Participants_Id",
                table: "Teams",
                column: "Id",
                principalTable: "Participants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Players_CaptainId",
                table: "Teams",
                column: "CaptainId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
