using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Users.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DailySummaryPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReceivesDailySummary",
                schema: "users",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceivesDailySummary",
                schema: "users",
                table: "users");
        }
    }
}
