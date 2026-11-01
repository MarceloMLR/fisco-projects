using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Projects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleItemNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "projects",
                table: "schedule_items",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                schema: "projects",
                table: "schedule_items");
        }
    }
}
