using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImageBatchSearchWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageBatchSearchJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProfileKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ManifestJson = table.Column<string>(type: "jsonb", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageBatchSearchJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImageBatchSearchAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AssetRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    ProviderInputUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageBatchSearchAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageBatchSearchAssets_ImageBatchSearchJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "ImageBatchSearchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageBatchSearchIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TargetKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    QueryType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageBatchSearchIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageBatchSearchIssues_ImageBatchSearchJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "ImageBatchSearchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageBatchSearchMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    QueryType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ImageKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ImageFileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    DetectedText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SourceProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageBatchSearchMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageBatchSearchMatches_ImageBatchSearchAssets_ImageAssetId",
                        column: x => x.ImageAssetId,
                        principalTable: "ImageBatchSearchAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImageBatchSearchMatches_ImageBatchSearchJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "ImageBatchSearchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageBatchSearchAssets_DeletedAt",
                table: "ImageBatchSearchAssets",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImageBatchSearchAssets_JobId_AssetKey",
                table: "ImageBatchSearchAssets",
                columns: new[] { "JobId", "AssetKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageBatchSearchIssues_JobId",
                table: "ImageBatchSearchIssues",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageBatchSearchJobs_Status_CreatedAt",
                table: "ImageBatchSearchJobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImageBatchSearchMatches_ImageAssetId",
                table: "ImageBatchSearchMatches",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageBatchSearchMatches_JobId_TargetKey_ImageKey",
                table: "ImageBatchSearchMatches",
                columns: new[] { "JobId", "TargetKey", "ImageKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageBatchSearchIssues");

            migrationBuilder.DropTable(
                name: "ImageBatchSearchMatches");

            migrationBuilder.DropTable(
                name: "ImageBatchSearchAssets");

            migrationBuilder.DropTable(
                name: "ImageBatchSearchJobs");
        }
    }
}
