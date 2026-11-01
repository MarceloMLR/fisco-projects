using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.TimeTracking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlannedHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "planned_hours",
                schema: "timetracking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planned_hours", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_planned_hours_ProjectId",
                schema: "timetracking",
                table: "planned_hours",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_planned_hours_ScheduleItemId",
                schema: "timetracking",
                table: "planned_hours",
                column: "ScheduleItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "planned_hours",
                schema: "timetracking");
        }
    }
}
