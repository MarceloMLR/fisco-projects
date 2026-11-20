using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenWare.Projects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StatusReportNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "status_report_notes",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Focus = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NextSteps = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ClientNeeds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_report_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_status_report_notes_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "status_report_risks",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketNumber = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Attention = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_report_risks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_status_report_risks_status_report_notes_NotesId",
                        column: x => x.NotesId,
                        principalSchema: "projects",
                        principalTable: "status_report_notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_status_report_notes_ProjectId",
                schema: "projects",
                table: "status_report_notes",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_status_report_risks_NotesId",
                schema: "projects",
                table: "status_report_risks",
                column: "NotesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "status_report_risks",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "status_report_notes",
                schema: "projects");
        }
    }
}
