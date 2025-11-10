using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MercuriusAPI.Migrations;

/// <inheritdoc />
public partial class BYEFlagPerParticipantInMatchInsteadOfGeneralBYEFlag : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "ContainsBYE",
            table: "Matches",
            newName: "Participant2IsBYE");

        migrationBuilder.AddColumn<bool>(
            name: "Participant1IsBYE",
            table: "Matches",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Participant1IsBYE",
            table: "Matches");

        migrationBuilder.RenameColumn(
            name: "Participant2IsBYE",
            table: "Matches",
            newName: "ContainsBYE");
    }
}
