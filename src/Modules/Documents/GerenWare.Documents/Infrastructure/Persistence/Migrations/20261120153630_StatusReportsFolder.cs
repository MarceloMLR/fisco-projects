using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenWare.Documents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StatusReportsFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStatusReport",
                schema: "documents",
                table: "folders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsStatusReport",
                schema: "documents",
                table: "default_folders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000001"),
                column: "IsStatusReport",
                value: false);

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000002"),
                column: "IsStatusReport",
                value: false);

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000003"),
                column: "IsStatusReport",
                value: false);

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000004"),
                column: "IsStatusReport",
                value: false);

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000005"),
                column: "IsStatusReport",
                value: false);

            migrationBuilder.UpdateData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000006"),
                column: "IsStatusReport",
                value: false);

            // The folder of the status reports (ADR-0165): a default folder that already has the name becomes it, active.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [documents].[default_folders] WHERE [Name] = N'Relatórios de status')
                    UPDATE [documents].[default_folders] SET [IsStatusReport] = 1, [IsActive] = 1
                    WHERE [Name] = N'Relatórios de status' AND [IsAcceptance] = 0;
                ELSE
                    INSERT INTO [documents].[default_folders] ([Id], [IsAcceptance], [IsActive], [IsStatusReport], [Name], [VisibleTo])
                    VALUES ('019a0a10-0000-7000-8000-000000000007', 0, 1, 1, N'Relatórios de status', 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "documents",
                table: "default_folders",
                keyColumn: "Id",
                keyValue: new Guid("019a0a10-0000-7000-8000-000000000007"));

            migrationBuilder.DropColumn(
                name: "IsStatusReport",
                schema: "documents",
                table: "folders");

            migrationBuilder.DropColumn(
                name: "IsStatusReport",
                schema: "documents",
                table: "default_folders");
        }
    }
}
