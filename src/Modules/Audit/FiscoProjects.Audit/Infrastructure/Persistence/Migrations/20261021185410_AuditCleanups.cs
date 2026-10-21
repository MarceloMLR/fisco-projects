using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditCleanups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cleanups",
                schema: "audit",
                columns: table => new
                {
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    RanAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KeptFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    Removed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cleanups", x => x.Month);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cleanups",
                schema: "audit");
        }
    }
}
