using Mercurius.LAN.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations;

[DbContext(typeof(MercuriusDBContext))]
[Migration("20260807111110_AddDiscoverySearchProjections")]
public partial class AddDiscoverySearchProjections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "discovery");

        migrationBuilder.CreateTable(
            name: "search_documents",
            schema: "discovery",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                subtitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                image_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                route = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                normalized_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                source_version = table.Column<long>(type: "bigint", nullable: false),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_search_documents", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "search_index_rebuild_jobs",
            schema: "discovery",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_search_index_rebuild_jobs", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_search_documents_entity_type_entity_id",
            schema: "discovery",
            table: "search_documents",
            columns: new[] { "entity_type", "entity_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_search_documents_is_deleted_entity_type_title_entity_id",
            schema: "discovery",
            table: "search_documents",
            columns: new[] { "is_deleted", "entity_type", "title", "entity_id" });

        migrationBuilder.CreateIndex(
            name: "IX_search_index_rebuild_jobs_status_created_at_utc",
            schema: "discovery",
            table: "search_index_rebuild_jobs",
            columns: new[] { "status", "created_at_utc" });

        migrationBuilder.Sql("""
            CREATE EXTENSION IF NOT EXISTS pg_trgm;
            CREATE INDEX "IX_search_documents_normalized_text_trgm"
            ON discovery.search_documents USING gin (normalized_text gin_trgm_ops)
            WHERE is_deleted = false;
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX "IX_search_index_rebuild_jobs_one_active"
            ON discovery.search_index_rebuild_jobs ((1))
            WHERE status IN ('Pending', 'Running');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "search_documents", schema: "discovery");
        migrationBuilder.DropTable(name: "search_index_rebuild_jobs", schema: "discovery");
    }
}
