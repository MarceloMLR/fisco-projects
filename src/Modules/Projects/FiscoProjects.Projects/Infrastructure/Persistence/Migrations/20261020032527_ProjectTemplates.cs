using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Projects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProjectTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_templates",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "project_template_dependencies",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredecessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuccessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_template_dependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_template_dependencies_project_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "projects",
                        principalTable: "project_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_template_items",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartOffset = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_template_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_template_items_project_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "projects",
                        principalTable: "project_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_template_phases",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_template_phases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_template_phases_project_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "projects",
                        principalTable: "project_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_template_dependencies_TemplateId",
                schema: "projects",
                table: "project_template_dependencies",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_project_template_items_TemplateId",
                schema: "projects",
                table: "project_template_items",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_project_template_phases_TemplateId",
                schema: "projects",
                table: "project_template_phases",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_project_templates_Name",
                schema: "projects",
                table: "project_templates",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_template_dependencies",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "project_template_items",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "project_template_phases",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "project_templates",
                schema: "projects");
        }
    }
}
