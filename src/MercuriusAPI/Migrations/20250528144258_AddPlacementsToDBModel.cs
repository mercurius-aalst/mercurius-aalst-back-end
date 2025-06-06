using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MercuriusAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPlacementsToDBModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlacementId",
                table: "Participants",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Placement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Place = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Placement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Placement_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Participants_PlacementId",
                table: "Participants",
                column: "PlacementId");

            migrationBuilder.CreateIndex(
                name: "IX_Placement_GameId",
                table: "Placement",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_Placement_PlacementId",
                table: "Participants",
                column: "PlacementId",
                principalTable: "Placement",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Participants_Placement_PlacementId",
                table: "Participants");

            migrationBuilder.DropTable(
                name: "Placement");

            migrationBuilder.DropIndex(
                name: "IX_Participants_PlacementId",
                table: "Participants");

            migrationBuilder.DropColumn(
                name: "PlacementId",
                table: "Participants");
        }
    }
}
