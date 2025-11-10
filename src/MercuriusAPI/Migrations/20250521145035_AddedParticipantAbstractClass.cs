using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MercuriusAPI.Migrations;

/// <inheritdoc />
public partial class AddedParticipantAbstractClass : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Matches_Teams_Team1Id",
            table: "Matches");

        migrationBuilder.DropForeignKey(
            name: "FK_Matches_Teams_Team2Id",
            table: "Matches");

        migrationBuilder.DropForeignKey(
            name: "FK_Matches_Teams_WinnerId",
            table: "Matches");

        migrationBuilder.DropForeignKey(
            name: "FK_PlayerTeam_Players_PlayersId",
            table: "PlayerTeam");

        migrationBuilder.DropForeignKey(
            name: "FK_PlayerTeam_Teams_TeamsId",
            table: "PlayerTeam");

        migrationBuilder.DropForeignKey(
            name: "FK_Teams_Players_CaptainId",
            table: "Teams");

        migrationBuilder.DropTable(
            name: "GamePlayer");

        migrationBuilder.DropTable(
            name: "GameTeam");

        migrationBuilder.DropTable(
            name: "Players");

        migrationBuilder.DropIndex(
            name: "IX_Matches_Team2Id",
            table: "Matches");

        migrationBuilder.DropPrimaryKey(
            name: "PK_Teams",
            table: "Teams");

        migrationBuilder.RenameTable(
            name: "Teams",
            newName: "Participants");

        migrationBuilder.RenameColumn(
            name: "Team2Id",
            table: "Matches",
            newName: "ParticipantType");

        migrationBuilder.RenameColumn(
            name: "Team1Id",
            table: "Matches",
            newName: "Participant2Id");

        migrationBuilder.RenameIndex(
            name: "IX_Matches_Team1Id",
            table: "Matches",
            newName: "IX_Matches_Participant2Id");

        migrationBuilder.RenameIndex(
            name: "IX_Teams_CaptainId",
            table: "Participants",
            newName: "IX_Participants_CaptainId");

        migrationBuilder.AddColumn<int>(
            name: "Pariticipant1Id",
            table: "Matches",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ParticipantType",
            table: "Games",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "Participants",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<int>(
            name: "CaptainId",
            table: "Participants",
            type: "integer",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AddColumn<string>(
            name: "DiscordId",
            table: "Participants",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Email",
            table: "Participants",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Firstname",
            table: "Participants",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Lastname",
            table: "Participants",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ParticipantType",
            table: "Participants",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "RiotId",
            table: "Participants",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SteamId",
            table: "Participants",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Username",
            table: "Participants",
            type: "text",
            nullable: true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_Participants",
            table: "Participants",
            column: "Id");

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

        migrationBuilder.CreateIndex(
            name: "IX_Matches_Pariticipant1Id",
            table: "Matches",
            column: "Pariticipant1Id");

        migrationBuilder.CreateIndex(
            name: "IX_GameParticipant_ParticipantsId",
            table: "GameParticipant",
            column: "ParticipantsId");

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_Participants_Pariticipant1Id",
            table: "Matches",
            column: "Pariticipant1Id",
            principalTable: "Participants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_Participants_Participant2Id",
            table: "Matches",
            column: "Participant2Id",
            principalTable: "Participants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_Participants_WinnerId",
            table: "Matches",
            column: "WinnerId",
            principalTable: "Participants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Participants_Participants_CaptainId",
            table: "Participants",
            column: "CaptainId",
            principalTable: "Participants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_PlayerTeam_Participants_PlayersId",
            table: "PlayerTeam",
            column: "PlayersId",
            principalTable: "Participants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_PlayerTeam_Participants_TeamsId",
            table: "PlayerTeam",
            column: "TeamsId",
            principalTable: "Participants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Matches_Participants_Pariticipant1Id",
            table: "Matches");

        migrationBuilder.DropForeignKey(
            name: "FK_Matches_Participants_Participant2Id",
            table: "Matches");

        migrationBuilder.DropForeignKey(
            name: "FK_Matches_Participants_WinnerId",
            table: "Matches");

        migrationBuilder.DropForeignKey(
            name: "FK_Participants_Participants_CaptainId",
            table: "Participants");

        migrationBuilder.DropForeignKey(
            name: "FK_PlayerTeam_Participants_PlayersId",
            table: "PlayerTeam");

        migrationBuilder.DropForeignKey(
            name: "FK_PlayerTeam_Participants_TeamsId",
            table: "PlayerTeam");

        migrationBuilder.DropTable(
            name: "GameParticipant");

        migrationBuilder.DropIndex(
            name: "IX_Matches_Pariticipant1Id",
            table: "Matches");

        migrationBuilder.DropPrimaryKey(
            name: "PK_Participants",
            table: "Participants");

        migrationBuilder.DropColumn(
            name: "Pariticipant1Id",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "ParticipantType",
            table: "Games");

        migrationBuilder.DropColumn(
            name: "DiscordId",
            table: "Participants");

        migrationBuilder.DropColumn(
            name: "Email",
            table: "Participants");

        migrationBuilder.DropColumn(
            name: "Firstname",
            table: "Participants");

        migrationBuilder.DropColumn(
            name: "Lastname",
            table: "Participants");

        migrationBuilder.DropColumn(
            name: "ParticipantType",
            table: "Participants");

        migrationBuilder.DropColumn(
            name: "RiotId",
            table: "Participants");

        migrationBuilder.DropColumn(
            name: "SteamId",
            table: "Participants");

        migrationBuilder.DropColumn(
            name: "Username",
            table: "Participants");

        migrationBuilder.RenameTable(
            name: "Participants",
            newName: "Teams");

        migrationBuilder.RenameColumn(
            name: "ParticipantType",
            table: "Matches",
            newName: "Team2Id");

        migrationBuilder.RenameColumn(
            name: "Participant2Id",
            table: "Matches",
            newName: "Team1Id");

        migrationBuilder.RenameIndex(
            name: "IX_Matches_Participant2Id",
            table: "Matches",
            newName: "IX_Matches_Team1Id");

        migrationBuilder.RenameIndex(
            name: "IX_Participants_CaptainId",
            table: "Teams",
            newName: "IX_Teams_CaptainId");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "Teams",
            type: "text",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "CaptainId",
            table: "Teams",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_Teams",
            table: "Teams",
            column: "Id");

        migrationBuilder.CreateTable(
            name: "GameTeam",
            columns: table => new
            {
                GamesId = table.Column<int>(type: "integer", nullable: false),
                TeamsId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GameTeam", x => new { x.GamesId, x.TeamsId });
                table.ForeignKey(
                    name: "FK_GameTeam_Games_GamesId",
                    column: x => x.GamesId,
                    principalTable: "Games",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_GameTeam_Teams_TeamsId",
                    column: x => x.TeamsId,
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Players",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
            });

        migrationBuilder.CreateTable(
            name: "GamePlayer",
            columns: table => new
            {
                GamesId = table.Column<int>(type: "integer", nullable: false),
                PlayersId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GamePlayer", x => new { x.GamesId, x.PlayersId });
                table.ForeignKey(
                    name: "FK_GamePlayer_Games_GamesId",
                    column: x => x.GamesId,
                    principalTable: "Games",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_GamePlayer_Players_PlayersId",
                    column: x => x.PlayersId,
                    principalTable: "Players",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Matches_Team2Id",
            table: "Matches",
            column: "Team2Id");

        migrationBuilder.CreateIndex(
            name: "IX_GamePlayer_PlayersId",
            table: "GamePlayer",
            column: "PlayersId");

        migrationBuilder.CreateIndex(
            name: "IX_GameTeam_TeamsId",
            table: "GameTeam",
            column: "TeamsId");

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_Teams_Team1Id",
            table: "Matches",
            column: "Team1Id",
            principalTable: "Teams",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_Teams_Team2Id",
            table: "Matches",
            column: "Team2Id",
            principalTable: "Teams",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_Teams_WinnerId",
            table: "Matches",
            column: "WinnerId",
            principalTable: "Teams",
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

        migrationBuilder.AddForeignKey(
            name: "FK_Teams_Players_CaptainId",
            table: "Teams",
            column: "CaptainId",
            principalTable: "Players",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
