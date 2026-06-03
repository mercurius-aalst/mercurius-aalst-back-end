using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class PublicSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS pg_trgm;""");
            migrationBuilder.Sql("""
                CREATE INDEX "IX_Users_NormalizedUsername_Search"
                ON "Users" USING gin ("NormalizedUsername" gin_trgm_ops)
                WHERE "NormalizedUsername" IS NOT NULL AND "IsDeleted" = false;
                """);
            migrationBuilder.Sql("""
                CREATE INDEX "IX_Teams_NormalizedName_Search"
                ON "Teams" USING gin ("NormalizedName" gin_trgm_ops);
                """);
            migrationBuilder.Sql("""
                CREATE INDEX "IX_Games_Name_Search"
                ON "Games" USING gin (lower("Name") gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX "IX_Games_Name_Search";""");
            migrationBuilder.Sql("""DROP INDEX "IX_Teams_NormalizedName_Search";""");
            migrationBuilder.Sql("""DROP INDEX "IX_Users_NormalizedUsername_Search";""");
        }
    }
}
