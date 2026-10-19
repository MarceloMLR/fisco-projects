using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Administration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "administration");

            migrationBuilder.CreateTable(
                name: "installation_settings",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    LogoFileId = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    LogoFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LogoContentType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LogoChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installation_settings", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "administration",
                table: "installation_settings",
                columns: new[] { "Id", "LogoChangedAt", "LogoContentType", "LogoFileId", "LogoFileName", "Name" },
                values: new object[] { new Guid("0199f2a0-6b5c-7d1e-9a42-3c8e5f7a1b20"), null, null, null, null, "FiscoProjects" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "installation_settings",
                schema: "administration");
        }
    }
}
