using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class ModuleEventingInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Teams",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "platform",
                columns: table => new
                {
                    consumer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.consumer_name, x.message_id });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_attempt_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_at_utc_occurred_at_utc",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "processed_at_utc", "occurred_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Teams");
        }
    }
}
