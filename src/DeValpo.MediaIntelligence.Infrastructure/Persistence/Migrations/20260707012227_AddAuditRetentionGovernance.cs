using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditRetentionGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "MediaAssets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ActorSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ActorClientKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResourceClientKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientRetentionPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientRetentionPolicies_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_DeletedAt",
                table: "MediaAssets",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CreatedAt",
                table: "AuditEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EventType",
                table: "AuditEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ResourceClientKey",
                table: "AuditEvents",
                column: "ResourceClientKey");

            migrationBuilder.CreateIndex(
                name: "IX_ClientRetentionPolicies_ClientId",
                table: "ClientRetentionPolicies",
                column: "ClientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "ClientRetentionPolicies");

            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_DeletedAt",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MediaAssets");
        }
    }
}
