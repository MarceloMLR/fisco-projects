using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Projects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersonAbsences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "person_absences",
                schema: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Start = table.Column<DateOnly>(type: "date", nullable: false),
                    End = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_absences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_person_absences_End",
                schema: "projects",
                table: "person_absences",
                column: "End");

            migrationBuilder.CreateIndex(
                name: "IX_person_absences_UserId",
                schema: "projects",
                table: "person_absences",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "person_absences",
                schema: "projects");
        }
    }
}
