using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Administration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InstallationMessageColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DevelopmentMessageColor",
                schema: "administration",
                table: "installation_settings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunctionalMessageColor",
                schema: "administration",
                table: "installation_settings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerMessageColor",
                schema: "administration",
                table: "installation_settings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerMessageColor",
                schema: "administration",
                table: "installation_settings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequesterMessageColor",
                schema: "administration",
                table: "installation_settings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechLeadMessageColor",
                schema: "administration",
                table: "installation_settings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "administration",
                table: "installation_settings",
                keyColumn: "Id",
                keyValue: new Guid("0199f2a0-6b5c-7d1e-9a42-3c8e5f7a1b20"),
                columns: new[] { "DevelopmentMessageColor", "FunctionalMessageColor", "ManagerMessageColor", "OwnerMessageColor", "RequesterMessageColor", "TechLeadMessageColor" },
                values: new object[] { null, null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DevelopmentMessageColor",
                schema: "administration",
                table: "installation_settings");

            migrationBuilder.DropColumn(
                name: "FunctionalMessageColor",
                schema: "administration",
                table: "installation_settings");

            migrationBuilder.DropColumn(
                name: "ManagerMessageColor",
                schema: "administration",
                table: "installation_settings");

            migrationBuilder.DropColumn(
                name: "OwnerMessageColor",
                schema: "administration",
                table: "installation_settings");

            migrationBuilder.DropColumn(
                name: "RequesterMessageColor",
                schema: "administration",
                table: "installation_settings");

            migrationBuilder.DropColumn(
                name: "TechLeadMessageColor",
                schema: "administration",
                table: "installation_settings");
        }
    }
}
