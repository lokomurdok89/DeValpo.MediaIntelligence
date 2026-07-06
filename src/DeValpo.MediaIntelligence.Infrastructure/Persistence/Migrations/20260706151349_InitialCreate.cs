using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProfileKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AssetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ClientKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DefinitionJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    RequiresHumanReview = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisResults_AnalysisJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "AnalysisJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HumanReviewTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanReviewTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HumanReviewTasks_AnalysisJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "AnalysisJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaAssets_AnalysisJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "AnalysisJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillExecutions_AnalysisJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "AnalysisJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceSkill = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisIssues_AnalysisResults_AnalysisResultId",
                        column: x => x.AnalysisResultId,
                        principalTable: "AnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetectedEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    SourceSkill = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectedEntities_AnalysisResults_AnalysisResultId",
                        column: x => x.AnalysisResultId,
                        principalTable: "AnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetectedLabels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    SourceSkill = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedLabels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectedLabels_AnalysisResults_AnalysisResultId",
                        column: x => x.AnalysisResultId,
                        principalTable: "AnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtractedFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Confidence = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    SourceSkill = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractedFields_AnalysisResults_AnalysisResultId",
                        column: x => x.AnalysisResultId,
                        principalTable: "AnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedClips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClipKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Confidence = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    SourceSkill = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedClips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedClips_AnalysisResults_AnalysisResultId",
                        column: x => x.AnalysisResultId,
                        principalTable: "AnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisIssues_AnalysisResultId",
                table: "AnalysisIssues",
                column: "AnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisJobs_ClientKey",
                table: "AnalysisJobs",
                column: "ClientKey");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisJobs_ProfileKey",
                table: "AnalysisJobs",
                column: "ProfileKey");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisJobs_Status",
                table: "AnalysisJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisProfiles_ProfileKey",
                table: "AnalysisProfiles",
                column: "ProfileKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_JobId",
                table: "AnalysisResults",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientKey",
                table: "Clients",
                column: "ClientKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetectedEntities_AnalysisResultId",
                table: "DetectedEntities",
                column: "AnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedLabels_AnalysisResultId",
                table: "DetectedLabels",
                column: "AnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedFields_AnalysisResultId",
                table: "ExtractedFields",
                column: "AnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedClips_AnalysisResultId",
                table: "GeneratedClips",
                column: "AnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanReviewTasks_JobId",
                table: "HumanReviewTasks",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_JobId",
                table: "MediaAssets",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillExecutions_JobId",
                table: "SkillExecutions",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_JobId",
                table: "WebhookDeliveries",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisIssues");

            migrationBuilder.DropTable(
                name: "AnalysisProfiles");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "DetectedEntities");

            migrationBuilder.DropTable(
                name: "DetectedLabels");

            migrationBuilder.DropTable(
                name: "ExtractedFields");

            migrationBuilder.DropTable(
                name: "GeneratedClips");

            migrationBuilder.DropTable(
                name: "HumanReviewTasks");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "SkillExecutions");

            migrationBuilder.DropTable(
                name: "WebhookDeliveries");

            migrationBuilder.DropTable(
                name: "AnalysisResults");

            migrationBuilder.DropTable(
                name: "AnalysisJobs");
        }
    }
}
