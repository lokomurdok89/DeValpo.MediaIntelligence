using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisJobProcessingAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcessingAttempts",
                table: "AnalysisJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingAttempts",
                table: "AnalysisJobs");
        }
    }
}
