using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Projects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Schedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "schedules",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedules_projects_Id",
                        column: x => x.Id,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_phases",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_phases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedule_phases_schedules_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "projects",
                        principalTable: "schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_items",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Start = table.Column<DateOnly>(type: "date", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    End = table.Column<DateOnly>(type: "date", nullable: false),
                    Progress = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedule_items_schedule_phases_PhaseId",
                        column: x => x.PhaseId,
                        principalSchema: "projects",
                        principalTable: "schedule_phases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_schedule_items_schedules_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "projects",
                        principalTable: "schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_dependencies",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredecessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuccessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_dependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedule_dependencies_schedule_items_PredecessorId",
                        column: x => x.PredecessorId,
                        principalSchema: "projects",
                        principalTable: "schedule_items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_schedule_dependencies_schedule_items_SuccessorId",
                        column: x => x.SuccessorId,
                        principalSchema: "projects",
                        principalTable: "schedule_items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_schedule_dependencies_schedules_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "projects",
                        principalTable: "schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_dependencies_PredecessorId_SuccessorId",
                schema: "projects",
                table: "schedule_dependencies",
                columns: new[] { "PredecessorId", "SuccessorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_dependencies_ProjectId",
                schema: "projects",
                table: "schedule_dependencies",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_dependencies_SuccessorId",
                schema: "projects",
                table: "schedule_dependencies",
                column: "SuccessorId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_items_OwnerId",
                schema: "projects",
                table: "schedule_items",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_items_PhaseId",
                schema: "projects",
                table: "schedule_items",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_items_ProjectId",
                schema: "projects",
                table: "schedule_items",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_phases_ProjectId",
                schema: "projects",
                table: "schedule_phases",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schedule_dependencies",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "schedule_items",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "schedule_phases",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "schedules",
                schema: "projects");
        }
    }
}
