using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MercuriusAPI.Migrations;

/// <inheritdoc />
public partial class FixPlacementsMapping : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Participants_Placement_PlacementId",
            table: "Participants");

        migrationBuilder.DropForeignKey(
            name: "FK_Placement_Games_GameId",
            table: "Placement");

        migrationBuilder.DropIndex(
            name: "IX_Participants_PlacementId",
            table: "Participants");

        migrationBuilder.DropPrimaryKey(
            name: "PK_Placement",
            table: "Placement");

        migrationBuilder.DropColumn(
            name: "PlacementId",
            table: "Participants");

        migrationBuilder.RenameTable(
            name: "Placement",
            newName: "Placements");

        migrationBuilder.RenameIndex(
            name: "IX_Placement_GameId",
            table: "Placements",
            newName: "IX_Placements_GameId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_Placements",
            table: "Placements",
            column: "Id");

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

        migrationBuilder.CreateIndex(
            name: "IX_ParticipantPlacement_PlacementId",
            table: "ParticipantPlacement",
            column: "PlacementId");

        migrationBuilder.AddForeignKey(
            name: "FK_Placements_Games_GameId",
            table: "Placements",
            column: "GameId",
            principalTable: "Games",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Placements_Games_GameId",
            table: "Placements");

        migrationBuilder.DropTable(
            name: "ParticipantPlacement");

        migrationBuilder.DropPrimaryKey(
            name: "PK_Placements",
            table: "Placements");

        migrationBuilder.RenameTable(
            name: "Placements",
            newName: "Placement");

        migrationBuilder.RenameIndex(
            name: "IX_Placements_GameId",
            table: "Placement",
            newName: "IX_Placement_GameId");

        migrationBuilder.AddColumn<int>(
            name: "PlacementId",
            table: "Participants",
            type: "integer",
            nullable: true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_Placement",
            table: "Placement",
            column: "Id");

        migrationBuilder.CreateIndex(
            name: "IX_Participants_PlacementId",
            table: "Participants",
            column: "PlacementId");

        migrationBuilder.AddForeignKey(
            name: "FK_Participants_Placement_PlacementId",
            table: "Participants",
            column: "PlacementId",
            principalTable: "Placement",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_Placement_Games_GameId",
            table: "Placement",
            column: "GameId",
            principalTable: "Games",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
