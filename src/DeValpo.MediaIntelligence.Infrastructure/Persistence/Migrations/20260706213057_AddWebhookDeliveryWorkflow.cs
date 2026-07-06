using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookDeliveryWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientKey",
                table: "WebhookDeliveries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientWebhookTargetId",
                table: "WebhookDeliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "WebhookDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "WebhookDeliveries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "WebhookDeliveries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastStatusCode",
                table: "WebhookDeliveries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "WebhookDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayloadJson",
                table: "WebhookDeliveries",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ClientWebhookTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SigningSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientWebhookTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientWebhookTargets_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_ClientKey",
                table: "WebhookDeliveries",
                column: "ClientKey");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_ClientWebhookTargetId",
                table: "WebhookDeliveries",
                column: "ClientWebhookTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_NextAttemptAt",
                table: "WebhookDeliveries",
                column: "NextAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_Status",
                table: "WebhookDeliveries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ClientWebhookTargets_ClientId_TargetKey",
                table: "ClientWebhookTargets",
                columns: new[] { "ClientId", "TargetKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WebhookDeliveries_ClientWebhookTargets_ClientWebhookTargetId",
                table: "WebhookDeliveries",
                column: "ClientWebhookTargetId",
                principalTable: "ClientWebhookTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WebhookDeliveries_ClientWebhookTargets_ClientWebhookTargetId",
                table: "WebhookDeliveries");

            migrationBuilder.DropTable(
                name: "ClientWebhookTargets");

            migrationBuilder.DropIndex(
                name: "IX_WebhookDeliveries_ClientKey",
                table: "WebhookDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_WebhookDeliveries_ClientWebhookTargetId",
                table: "WebhookDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_WebhookDeliveries_NextAttemptAt",
                table: "WebhookDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_WebhookDeliveries_Status",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "ClientKey",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "ClientWebhookTargetId",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "LastStatusCode",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "PayloadJson",
                table: "WebhookDeliveries");
        }
    }
}
