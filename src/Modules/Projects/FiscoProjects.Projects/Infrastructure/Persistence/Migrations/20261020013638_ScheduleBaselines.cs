using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Projects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleBaselines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "schedule_baselines",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SavedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Start = table.Column<DateOnly>(type: "date", nullable: false),
                    End = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_baselines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedule_baselines_schedules_Id",
                        column: x => x.Id,
                        principalSchema: "projects",
                        principalTable: "schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_baseline_items",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Start = table.Column<DateOnly>(type: "date", nullable: false),
                    End = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_baseline_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedule_baseline_items_schedule_baselines_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "projects",
                        principalTable: "schedule_baselines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_baseline_items_ProjectId_ItemId",
                schema: "projects",
                table: "schedule_baseline_items",
                columns: new[] { "ProjectId", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schedule_baseline_items",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "schedule_baselines",
                schema: "projects");
        }
    }
}
