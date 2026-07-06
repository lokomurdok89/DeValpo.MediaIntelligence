using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHumanReviewWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "HumanReviewTasks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "HumanReviewTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectedFieldsJson",
                table: "HumanReviewTasks",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Decision",
                table: "HumanReviewTasks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatorId",
                table: "HumanReviewTasks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFieldsJson",
                table: "HumanReviewTasks",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "HumanReviewTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "HumanReviewTasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateIndex(
                name: "IX_HumanReviewTasks_Status",
                table: "HumanReviewTasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HumanReviewTasks_Status",
                table: "HumanReviewTasks");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "HumanReviewTasks");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "HumanReviewTasks");

            migrationBuilder.DropColumn(
                name: "CorrectedFieldsJson",
                table: "HumanReviewTasks");

            migrationBuilder.DropColumn(
                name: "Decision",
                table: "HumanReviewTasks");

            migrationBuilder.DropColumn(
                name: "OperatorId",
                table: "HumanReviewTasks");

            migrationBuilder.DropColumn(
                name: "OriginalFieldsJson",
                table: "HumanReviewTasks");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "HumanReviewTasks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "HumanReviewTasks");
        }
    }
}
