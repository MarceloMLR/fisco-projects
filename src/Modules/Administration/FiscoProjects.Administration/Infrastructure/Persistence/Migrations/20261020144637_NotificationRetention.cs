using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Administration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificationRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NotificationRetentionDays",
                schema: "administration",
                table: "installation_settings",
                type: "int",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.UpdateData(
                schema: "administration",
                table: "installation_settings",
                keyColumn: "Id",
                keyValue: new Guid("0199f2a0-6b5c-7d1e-9a42-3c8e5f7a1b20"),
                column: "NotificationRetentionDays",
                value: 90);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationRetentionDays",
                schema: "administration",
                table: "installation_settings");
        }
    }
}
