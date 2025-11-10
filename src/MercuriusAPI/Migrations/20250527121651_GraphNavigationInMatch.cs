using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MercuriusAPI.Migrations;

/// <inheritdoc />
public partial class GraphNavigationInMatch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "LoserId",
            table: "Matches",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "LoserNextMatchId",
            table: "Matches",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "WinnerNextMatchId",
            table: "Matches",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Matches_LoserId",
            table: "Matches",
            column: "LoserId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_LoserNextMatchId",
            table: "Matches",
            column: "LoserNextMatchId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_WinnerNextMatchId",
            table: "Matches",
            column: "WinnerNextMatchId");

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_Matches_LoserNextMatchId",
            table: "Matches",
            column: "LoserNextMatchId",
            principalTable: "Matches",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_Matches_WinnerNextMatchId",
            table: "Matches",
            column: "WinnerNextMatchId",
            principalTable: "Matches",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_Matches_Participants_LoserId",
            table: "Matches",
            column: "LoserId",
            principalTable: "Participants",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Matches_Matches_LoserNextMatchId",
            table: "Matches");

        migrationBuilder.DropForeignKey(
            name: "FK_Matches_Matches_WinnerNextMatchId",
            table: "Matches");

        migrationBuilder.DropForeignKey(
            name: "FK_Matches_Participants_LoserId",
            table: "Matches");

        migrationBuilder.DropIndex(
            name: "IX_Matches_LoserId",
            table: "Matches");

        migrationBuilder.DropIndex(
            name: "IX_Matches_LoserNextMatchId",
            table: "Matches");

        migrationBuilder.DropIndex(
            name: "IX_Matches_WinnerNextMatchId",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "LoserId",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "LoserNextMatchId",
            table: "Matches");

        migrationBuilder.DropColumn(
            name: "WinnerNextMatchId",
            table: "Matches");
    }
}
