using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Administration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InstallationSecondaryColorAndDarkLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DarkLogoChangedAt",
                schema: "administration",
                table: "installation_settings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DarkLogoContentType",
                schema: "administration",
                table: "installation_settings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DarkLogoFileId",
                schema: "administration",
                table: "installation_settings",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DarkLogoFileName",
                schema: "administration",
                table: "installation_settings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
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
                columns: new[] { "DarkLogoChangedAt", "DarkLogoContentType", "DarkLogoFileId", "DarkLogoFileName", "SecondaryColor" },
                values: new object[] { null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DarkLogoChangedAt",
                schema: "administration",
                table: "installation_settings");

            migrationBuilder.DropColumn(
                name: "DarkLogoContentType",
                schema: "administration",
                table: "installation_settings");

            migrationBuilder.DropColumn(
                name: "DarkLogoFileId",
                schema: "administration",
                table: "installation_settings");

            migrationBuilder.DropColumn(
                name: "DarkLogoFileName",
                schema: "administration",
                table: "installation_settings");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                schema: "administration",
                table: "installation_settings");
        }
    }
}
