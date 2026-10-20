using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NoticesAndSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_summaries",
                schema: "notifications",
                columns: table => new
                {
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Emails = table.Column<int>(type: "int", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_summaries", x => x.Day);
                });

            migrationBuilder.CreateTable(
                name: "digest_items",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Values = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digest_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_digest_items_CreatedAt",
                schema: "notifications",
                table: "digest_items",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_digest_items_UserId_Key",
                schema: "notifications",
                table: "digest_items",
                columns: new[] { "UserId", "Key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_summaries",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "digest_items",
                schema: "notifications");
        }
    }
}
