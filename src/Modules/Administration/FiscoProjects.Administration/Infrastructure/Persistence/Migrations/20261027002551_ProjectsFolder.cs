using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Administration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProjectsFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectsFolder",
                schema: "administration",
                table: "installation_settings",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                schema: "administration",
                table: "installation_settings",
                keyColumn: "Id",
                keyValue: new Guid("0199f2a0-6b5c-7d1e-9a42-3c8e5f7a1b20"),
                column: "ProjectsFolder",
                value: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectsFolder",
                schema: "administration",
                table: "installation_settings");
        }
    }
}
