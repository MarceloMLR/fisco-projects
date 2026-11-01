using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Projects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleLinkTypeAndLag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The dependencies there are stay "finishes, then starts" without lag (ADR-0137).
            migrationBuilder.AddColumn<int>(
                name: "Lag",
                schema: "projects",
                table: "schedule_dependencies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "projects",
                table: "schedule_dependencies",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "FinishToStart");

            migrationBuilder.AddColumn<int>(
                name: "Lag",
                schema: "projects",
                table: "project_template_dependencies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "projects",
                table: "project_template_dependencies",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "FinishToStart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lag",
                schema: "projects",
                table: "schedule_dependencies");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "projects",
                table: "schedule_dependencies");

            migrationBuilder.DropColumn(
                name: "Lag",
                schema: "projects",
                table: "project_template_dependencies");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "projects",
                table: "project_template_dependencies");
        }
    }
}
