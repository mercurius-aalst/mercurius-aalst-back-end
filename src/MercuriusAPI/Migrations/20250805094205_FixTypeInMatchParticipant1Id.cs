using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MercuriusAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixTypeInMatchParticipant1Id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Participants_Pariticipant1Id",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "Pariticipant1Id",
                table: "Matches",
                newName: "Participant1Id");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_Pariticipant1Id",
                table: "Matches",
                newName: "IX_Matches_Participant1Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Participants_Participant1Id",
                table: "Matches",
                column: "Participant1Id",
                principalTable: "Participants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Participants_Participant1Id",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "Participant1Id",
                table: "Matches",
                newName: "Pariticipant1Id");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_Participant1Id",
                table: "Matches",
                newName: "IX_Matches_Pariticipant1Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Participants_Pariticipant1Id",
                table: "Matches",
                column: "Pariticipant1Id",
                principalTable: "Participants",
                principalColumn: "Id");
        }
    }
}
