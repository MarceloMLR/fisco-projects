using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Projects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProjectFolderChosen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StorageFolderChosen",
                schema: "projects",
                table: "projects",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageFolderChosen",
                schema: "projects",
                table: "projects");
        }
    }
}
