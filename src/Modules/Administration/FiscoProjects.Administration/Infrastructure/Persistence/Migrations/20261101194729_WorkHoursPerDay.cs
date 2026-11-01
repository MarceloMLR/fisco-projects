using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Administration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkHoursPerDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WorkHoursPerDay",
                schema: "administration",
                table: "installation_settings",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 8m);

            migrationBuilder.UpdateData(
                schema: "administration",
                table: "installation_settings",
                keyColumn: "Id",
                keyValue: new Guid("0199f2a0-6b5c-7d1e-9a42-3c8e5f7a1b20"),
                column: "WorkHoursPerDay",
                value: 8m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkHoursPerDay",
                schema: "administration",
                table: "installation_settings");
        }
    }
}
