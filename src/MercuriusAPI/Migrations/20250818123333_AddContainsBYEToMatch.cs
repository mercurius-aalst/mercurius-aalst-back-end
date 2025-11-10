using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MercuriusAPI.Migrations;

/// <inheritdoc />
public partial class AddContainsBYEToMatch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ContainsBYE",
            table: "Matches",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ContainsBYE",
            table: "Matches");
    }
}
