using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MercuriusAPI.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Games",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "text", nullable: false),
                StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                BracketType = table.Column<int>(type: "integer", nullable: false),
                Format = table.Column<string>(type: "text", nullable: false),
                FinalsFormat = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Games", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Players",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Players", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Teams",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Teams", x => x.Id);
            });

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
            name: "Matches",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                BracketType = table.Column<int>(type: "integer", nullable: false),
                RoundNumber = table.Column<int>(type: "integer", nullable: false),
                MatchNumber = table.Column<int>(type: "integer", nullable: false),
                GameId = table.Column<int>(type: "integer", nullable: false),
                Team1Id = table.Column<int>(type: "integer", nullable: false),
                Team2Id = table.Column<int>(type: "integer", nullable: false),
                WinnerId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Matches", x => x.Id);
                table.ForeignKey(
                    name: "FK_Matches_Games_GameId",
                    column: x => x.GameId,
                    principalTable: "Games",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Matches_Teams_Team1Id",
                    column: x => x.Team1Id,
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Matches_Teams_Team2Id",
                    column: x => x.Team2Id,
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Matches_Teams_WinnerId",
                    column: x => x.WinnerId,
                    principalTable: "Teams",
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
            name: "IX_GameTeam_TeamsId",
            table: "GameTeam",
            column: "TeamsId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_GameId",
            table: "Matches",
            column: "GameId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_Team1Id",
            table: "Matches",
            column: "Team1Id");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_Team2Id",
            table: "Matches",
            column: "Team2Id");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_WinnerId",
            table: "Matches",
            column: "WinnerId");

        migrationBuilder.CreateIndex(
            name: "IX_PlayerTeam_TeamsId",
            table: "PlayerTeam",
            column: "TeamsId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "GameTeam");

        migrationBuilder.DropTable(
            name: "Matches");

        migrationBuilder.DropTable(
            name: "PlayerTeam");

        migrationBuilder.DropTable(
            name: "Games");

        migrationBuilder.DropTable(
            name: "Players");

        migrationBuilder.DropTable(
            name: "Teams");
    }
}
