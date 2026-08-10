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
                name: "lease_expires_at_utc",
                schema: "platform",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "lease_id",
                schema: "platform",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at_utc",
                schema: "platform",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_dead_letter_retention",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "dead_lettered_at_utc", "id" },
                filter: "dead_lettered_at_utc IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending_claim",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "next_attempt_at_utc", "lease_expires_at_utc", "occurred_at_utc", "id" },
                filter: "processed_at_utc IS NULL AND dead_lettered_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_retention",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "processed_at_utc", "id" },
                filter: "processed_at_utc IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_message_id",
                schema: "platform",
                table: "inbox_messages",
                column: "message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_dead_letter_retention",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending_claim",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_processed_retention",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_inbox_messages_message_id",
                schema: "platform",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "dead_lettered_at_utc",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "lease_expires_at_utc",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "lease_id",
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
