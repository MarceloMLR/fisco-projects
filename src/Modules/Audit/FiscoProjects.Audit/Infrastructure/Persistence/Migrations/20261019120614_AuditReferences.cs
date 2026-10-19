using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItemLabelReference",
                schema: "audit",
                table: "entries",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemLabelReference",
                schema: "audit",
                table: "entries");
        }
    }
}
