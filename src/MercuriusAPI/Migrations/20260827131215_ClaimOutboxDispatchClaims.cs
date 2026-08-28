using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercurius.LAN.API.Migrations
{
    /// <inheritdoc />
    public partial class ClaimOutboxDispatchClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending_dispatch",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.AddColumn<DateTime>(
                name: "claim_expires_at_utc",
                schema: "platform",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "claim_token",
                schema: "platform",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending_dispatch",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "next_attempt_at_utc", "claim_expires_at_utc", "occurred_at_utc", "id" },
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
                name: "claim_expires_at_utc",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "claim_token",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending_dispatch",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "next_attempt_at_utc", "occurred_at_utc", "id" },
                filter: "processed_at_utc IS NULL AND dead_lettered_at_utc IS NULL");
        }
    }
}
