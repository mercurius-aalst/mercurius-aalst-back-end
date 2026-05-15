using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class SponsorPlacementContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Sponsors"
                ALTER COLUMN "SponsorTier" TYPE text
                USING CASE "SponsorTier"
                    WHEN 0 THEN 'Presenting'
                    WHEN 1 THEN 'Gold'
                    WHEN 2 THEN 'Silver'
                    ELSE 'Bronze'
                END;
                """);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Sponsors",
                type: "character varying(1200)",
                maxLength: 1200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GameSponsorPlacements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    SponsorId = table.Column<int>(type: "integer", nullable: false),
                    Context = table.Column<string>(type: "text", nullable: false),
                    Headline = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    SupportLine = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSponsorPlacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSponsorPlacements_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameSponsorPlacements_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameSponsorPlacements_GameId_Context_DisplayOrder_Id",
                table: "GameSponsorPlacements",
                columns: new[] { "GameId", "Context", "DisplayOrder", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_GameSponsorPlacements_SponsorId",
                table: "GameSponsorPlacements",
                column: "SponsorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameSponsorPlacements");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Sponsors");

            migrationBuilder.Sql("""
                ALTER TABLE "Sponsors"
                ALTER COLUMN "SponsorTier" TYPE integer
                USING CASE "SponsorTier"
                    WHEN 'Presenting' THEN 0
                    WHEN 'Gold' THEN 1
                    WHEN 'Silver' THEN 2
                    ELSE 3
                END;
                """);
        }
    }
}
