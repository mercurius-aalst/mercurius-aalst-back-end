using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class DiscoverySearchQueryIndexOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS discovery."IX_search_documents_active_exact_order";
                DROP INDEX IF EXISTS discovery."IX_search_documents_active_prefix";
                DROP INDEX IF EXISTS discovery."IX_search_documents_normalized_text_trgm";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_text",
                schema: "discovery",
                table: "search_documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.CreateIndex(
                name: "IX_search_documents_active_text",
                schema: "discovery",
                table: "search_documents",
                columns: new[] { "normalized_text", "type_order", "entity_id" },
                filter: "is_deleted = false");

            migrationBuilder.Sql("""
                CREATE INDEX "IX_search_documents_normalized_text_trgm"
                ON discovery.search_documents USING gin (normalized_text gin_trgm_ops)
                WHERE is_deleted = false;

                ALTER TABLE discovery.search_documents
                    SET (fillfactor = 90, autovacuum_vacuum_scale_factor = 0.05);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS discovery."IX_search_documents_active_text";
                DROP INDEX IF EXISTS discovery."IX_search_documents_normalized_text_trgm";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_text",
                schema: "discovery",
                table: "search_documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldCollation: "C");

            migrationBuilder.CreateIndex(
                name: "IX_search_documents_active_exact_order",
                schema: "discovery",
                table: "search_documents",
                columns: new[] { "normalized_text", "type_order", "entity_id" },
                filter: "is_deleted = false AND entity_type IN ('user', 'team', 'tournament')");

            migrationBuilder.Sql("""
                ALTER TABLE discovery.search_documents
                    RESET (fillfactor, autovacuum_vacuum_scale_factor);

                CREATE INDEX "IX_search_documents_active_prefix"
                ON discovery.search_documents (normalized_text text_pattern_ops, type_order, entity_id)
                WHERE is_deleted = false AND entity_type IN ('user', 'team', 'game');

                CREATE INDEX "IX_search_documents_normalized_text_trgm"
                ON discovery.search_documents USING gin (normalized_text gin_trgm_ops)
                WHERE is_deleted = false AND entity_type IN ('user', 'team', 'game');
                """);
        }
    }
}
