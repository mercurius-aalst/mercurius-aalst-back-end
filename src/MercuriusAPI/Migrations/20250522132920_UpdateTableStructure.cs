using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MercuriusAPI.Migrations;

/// <inheritdoc />
public partial class UpdateTableStructure : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Participants_Participants_CaptainId",
            table: "Participants");

        migrationBuilder.DropForeignKey(
            name: "FK_PlayerTeam_Participants_PlayersId",
            table: "PlayerTeam");

        migrationBuilder.DropForeignKey(
            name: "FK_PlayerTeam_Participants_TeamsId",
            table: "PlayerTeam");

        migrationBuilder.DropIndex(
            name: "IX_Participants_CaptainId",
            table: "Participants");

        migrationBuilder.DropColumn(
            name: "CaptainId",
            table: "Participants");

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
            name: "Name",
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

        migrationBuilder.CreateTable(
            name: "Players",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false),
                Username = table.Column<string>(type: "text", nullable: false),
                Firstname = table.Column<string>(type: "text", nullable: false),
                Lastname = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "text", nullable: false),
                DiscordId = table.Column<string>(type: "text", nullable: true),
                SteamId = table.Column<string>(type: "text", nullable: true),
                RiotId = table.Column<string>(type: "text", nullable: true)
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
            name: "Teams",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                CaptainId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Teams", x => x.Id);
                table.ForeignKey(
                    name: "FK_Teams_Participants_Id",
                    column: x => x.Id,
                    principalTable: "Participants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Teams_Players_CaptainId",
                    column: x => x.CaptainId,
                    principalTable: "Players",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Teams_CaptainId",
            table: "Teams",
            column: "CaptainId");

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

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_PlayerTeam_Players_PlayersId",
            table: "PlayerTeam");

        migrationBuilder.DropForeignKey(
            name: "FK_PlayerTeam_Teams_TeamsId",
            table: "PlayerTeam");

        migrationBuilder.DropTable(
            name: "Teams");

        migrationBuilder.DropTable(
            name: "Players");

        migrationBuilder.AddColumn<int>(
            name: "CaptainId",
            table: "Participants",
            type: "integer",
            nullable: true);

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

        migrationBuilder.AddColumn<string>(
            name: "Name",
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

        migrationBuilder.CreateIndex(
            name: "IX_Participants_CaptainId",
            table: "Participants",
            column: "CaptainId");

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
}
