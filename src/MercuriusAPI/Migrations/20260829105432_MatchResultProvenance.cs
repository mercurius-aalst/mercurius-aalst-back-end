using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class MatchResultProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Participant1SourceMatchId",
                schema: "tournament",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Participant2SourceMatchId",
                schema: "tournament",
                table: "matches",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Participant1SourceMatchId",
                schema: "tournament",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Participant2SourceMatchId",
                schema: "tournament",
                table: "matches");
        }
    }
}
