using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class HardenModuleEventDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_processed_at_utc_occurred_at_utc",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.AddColumn<DateTime>(
                name: "dead_lettered_at_utc",
                schema: "platform",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at_utc",
                schema: "platform",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending_dispatch",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "next_attempt_at_utc", "occurred_at_utc", "id" },
                filter: "processed_at_utc IS NULL AND dead_lettered_at_utc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending_dispatch",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "dead_lettered_at_utc",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "next_attempt_at_utc",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_at_utc_occurred_at_utc",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "processed_at_utc", "occurred_at_utc" });
        }
    }
}
