using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    [Migration("20260605204500_AuthenticatedUserSearchIndexes")]
    public partial class AuthenticatedUserSearchIndexes : Migration
    {
        private const string ActiveCompleteUserPredicate = """
            "NormalizedUsername" IS NOT NULL
            AND "Username" IS NOT NULL
            AND "Username" <> ''
            AND "NormalizedUsername" <> ''
            AND "IsDeleted" = false
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS pg_trgm;""");
            migrationBuilder.Sql($"""
                CREATE INDEX "IX_Users_AuthenticatedSearch_NormalizedUsername_Trgm"
                ON "Users" USING gin ("NormalizedUsername" gin_trgm_ops)
                WHERE {ActiveCompleteUserPredicate};
                """);
            migrationBuilder.Sql($"""
                CREATE INDEX "IX_Users_AuthenticatedSearch_Cursor"
                ON "Users" ("NormalizedUsername", "Id")
                WHERE {ActiveCompleteUserPredicate};
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX "IX_Users_AuthenticatedSearch_Cursor";""");
            migrationBuilder.Sql("""DROP INDEX "IX_Users_AuthenticatedSearch_NormalizedUsername_Trgm";""");
        }
    }
}
