using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Documents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FolderVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VisibleTo",
                schema: "documents",
                table: "folders",
                type: "int",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddColumn<int>(
                name: "VisibleTo",
                schema: "documents",
                table: "default_folders",
                type: "int",
                nullable: false,
                defaultValue: 15);

            // Every folder that exists is seen by the four roles, as before (ADR-0148); the folder of the terms only by
            // the managers (ADR-0129).
            migrationBuilder.Sql("UPDATE [documents].[folders] SET [VisibleTo] = 0 WHERE [IsAcceptance] = 1;");
            migrationBuilder.Sql("UPDATE [documents].[default_folders] SET [VisibleTo] = 0 WHERE [IsAcceptance] = 1;");

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000001"),
                column: "VisibleTo",
                value: 15);

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000002"),
                column: "VisibleTo",
                value: 15);

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000003"),
                column: "VisibleTo",
                value: 15);

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000004"),
                column: "VisibleTo",
                value: 15);

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000005"),
                column: "VisibleTo",
                value: 15);

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000006"),
                column: "VisibleTo",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisibleTo",
                schema: "documents",
                table: "folders");

            migrationBuilder.DropColumn(
                name: "VisibleTo",
                schema: "documents",
                table: "default_folders");
        }
    }
}
