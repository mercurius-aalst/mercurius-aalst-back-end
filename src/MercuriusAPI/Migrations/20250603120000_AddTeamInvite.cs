using Microsoft.EntityFrameworkCore.Migrations;

namespace MercuriusAPI.Migrations;

public partial class AddTeamInvite : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TeamInvites",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TeamId = table.Column<int>(nullable: false),
                PlayerId = table.Column<int>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                Status = table.Column<int>(nullable: false),
                RespondedAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TeamInvites", x => x.Id);
                table.ForeignKey(
                    name: "FK_TeamInvites_Teams_TeamId",
                    column: x => x.TeamId,
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TeamInvites_Players_PlayerId",
                    column: x => x.PlayerId,
                    principalTable: "Players",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(
            name: "IX_TeamInvites_TeamId",
            table: "TeamInvites",
            column: "TeamId");
        migrationBuilder.CreateIndex(
            name: "IX_TeamInvites_PlayerId",
            table: "TeamInvites",
            column: "PlayerId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TeamInvites");
    }
}