using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MercuriusAPI.Migrations;

/// <inheritdoc />
public partial class RemoveIsRevokedFromRefreshToken : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsRevoked",
            table: "RefreshTokens");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsRevoked",
            table: "RefreshTokens",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }
}
