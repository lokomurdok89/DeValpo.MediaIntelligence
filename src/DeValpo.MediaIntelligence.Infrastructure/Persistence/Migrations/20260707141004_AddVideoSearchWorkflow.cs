using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoSearchWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoSearchJobs",
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
                    table.PrimaryKey("PK_VideoSearchJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VideoSearchAssets",
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
                    VideoDurationSeconds = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoSearchAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoSearchAssets_VideoSearchJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "VideoSearchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoSearchIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoSearchIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoSearchIssues_VideoSearchJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "VideoSearchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoSearchFaceOccurrenceSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VideoFileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    StartTimeSeconds = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    EndTimeSeconds = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SourceProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoSearchFaceOccurrenceSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoSearchFaceOccurrenceSegments_VideoSearchAssets_VideoAs~",
                        column: x => x.VideoAssetId,
                        principalTable: "VideoSearchAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoSearchFaceOccurrenceSegments_VideoSearchJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "VideoSearchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoSearchTargetMatchSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VisibleNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VideoKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VideoFileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    StartTimeSeconds = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    EndTimeSeconds = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SourceProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoSearchTargetMatchSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoSearchTargetMatchSegments_VideoSearchAssets_VideoAsset~",
                        column: x => x.VideoAssetId,
                        principalTable: "VideoSearchAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoSearchTargetMatchSegments_VideoSearchJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "VideoSearchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoSearchAssets_DeletedAt",
                table: "VideoSearchAssets",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VideoSearchAssets_JobId_AssetKey",
                table: "VideoSearchAssets",
                columns: new[] { "JobId", "AssetKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoSearchFaceOccurrenceSegments_JobId_VideoKey",
                table: "VideoSearchFaceOccurrenceSegments",
                columns: new[] { "JobId", "VideoKey" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoSearchFaceOccurrenceSegments_VideoAssetId",
                table: "VideoSearchFaceOccurrenceSegments",
                column: "VideoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoSearchIssues_JobId",
                table: "VideoSearchIssues",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoSearchJobs_Status_CreatedAt",
                table: "VideoSearchJobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoSearchTargetMatchSegments_JobId_TargetKey_VideoKey",
                table: "VideoSearchTargetMatchSegments",
                columns: new[] { "JobId", "TargetKey", "VideoKey" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoSearchTargetMatchSegments_VideoAssetId",
                table: "VideoSearchTargetMatchSegments",
                column: "VideoAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoSearchFaceOccurrenceSegments");

            migrationBuilder.DropTable(
                name: "VideoSearchIssues");

            migrationBuilder.DropTable(
                name: "VideoSearchTargetMatchSegments");

            migrationBuilder.DropTable(
                name: "VideoSearchAssets");

            migrationBuilder.DropTable(
                name: "VideoSearchJobs");
        }
    }
}
