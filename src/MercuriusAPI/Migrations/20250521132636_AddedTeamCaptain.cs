using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MercuriusAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddedTeamCaptain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CaptainId",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Games\" SET \"Format\" = '0' WHERE \"Format\" IS NULL OR \"Format\" !~ '^[0-9]+$'");
            migrationBuilder.Sql("ALTER TABLE \"Games\" ALTER COLUMN \"Format\" TYPE integer USING (\"Format\"::integer)");

            migrationBuilder.Sql("UPDATE \"Games\" SET \"FinalsFormat\" = '0' WHERE \"FinalsFormat\" IS NULL OR \"FinalsFormat\" !~ '^[0-9]+$'");
            migrationBuilder.Sql("ALTER TABLE \"Games\" ALTER COLUMN \"FinalsFormat\" TYPE integer USING (\"FinalsFormat\"::integer)");

            migrationBuilder.AlterColumn<int>(
                name: "Format",
                table: "Games",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "FinalsFormat",
                table: "Games",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CaptainId",
                table: "Teams",
                column: "CaptainId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Players_CaptainId",
                table: "Teams",
                column: "CaptainId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Players_CaptainId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_CaptainId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CaptainId",
                table: "Teams");

            migrationBuilder.AlterColumn<string>(
                name: "Format",
                table: "Games",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "FinalsFormat",
                table: "Games",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
