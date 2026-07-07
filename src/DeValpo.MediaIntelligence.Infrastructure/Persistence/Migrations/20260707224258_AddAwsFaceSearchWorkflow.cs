using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAwsFaceSearchWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConsentReferenceId",
                table: "VideoSearchJobs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VideoSearchFaceReferenceMatchSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReferenceImageKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VideoKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VideoFileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    StartTimeSeconds = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    EndTimeSeconds = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Similarity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MatchType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoSearchFaceReferenceMatchSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoSearchFaceReferenceMatchSegments_VideoSearchAssets_Vid~",
                        column: x => x.VideoAssetId,
                        principalTable: "VideoSearchAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoSearchFaceReferenceMatchSegments_VideoSearchJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "VideoSearchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoSearchFaceReferenceMatchSegments_JobId_TargetKey_Video~",
                table: "VideoSearchFaceReferenceMatchSegments",
                columns: new[] { "JobId", "TargetKey", "VideoKey" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoSearchFaceReferenceMatchSegments_VideoAssetId",
                table: "VideoSearchFaceReferenceMatchSegments",
                column: "VideoAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoSearchFaceReferenceMatchSegments");

            migrationBuilder.DropColumn(
                name: "ConsentReferenceId",
                table: "VideoSearchJobs");
        }
    }
}
