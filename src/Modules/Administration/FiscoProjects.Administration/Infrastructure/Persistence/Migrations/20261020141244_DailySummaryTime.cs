using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Administration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DailySummaryTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "DailySummaryAt",
                schema: "administration",
                table: "installation_settings",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(7, 30, 0));

            migrationBuilder.UpdateData(
                schema: "administration",
                table: "installation_settings",
                keyColumn: "Id",
                keyValue: new Guid("0199f2a0-6b5c-7d1e-9a42-3c8e5f7a1b20"),
                column: "DailySummaryAt",
                value: new TimeOnly(7, 30, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailySummaryAt",
                schema: "administration",
                table: "installation_settings");
        }
    }
}
