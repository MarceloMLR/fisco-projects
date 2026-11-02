using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TeamsNotices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notice_preferences",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notice_preferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "teams_app",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManifestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoticesOn = table.Column<bool>(type: "bit", nullable: false),
                    PackageVersion = table.Column<int>(type: "int", nullable: false),
                    PackageHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_app", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "teams_notices",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Values = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlsoByEmail = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailedInstead = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_notices", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "notifications",
                table: "teams_app",
                columns: new[] { "Id", "ManifestId", "NoticesOn", "PackageHash", "PackageVersion" },
                values: new object[] { new Guid("0193f0a0-7ea0-7c3e-9a51-2f6b8c1d4e01"), new Guid("00000000-0000-0000-0000-000000000000"), false, null, 0 });

            // Each installation gets an application of Teams of its own, so two installations in one tenant never share it.
            migrationBuilder.Sql("UPDATE [notifications].[teams_app] SET [ManifestId] = NEWID();");

            migrationBuilder.CreateIndex(
                name: "IX_notice_preferences_UserId",
                schema: "notifications",
                table: "notice_preferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_notices_ProjectId",
                schema: "notifications",
                table: "teams_notices",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_teams_notices_Status_NextAttemptAt",
                schema: "notifications",
                table: "teams_notices",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_teams_notices_UserId",
                schema: "notifications",
                table: "teams_notices",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notice_preferences",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "teams_app",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "teams_notices",
                schema: "notifications");
        }
    }
}
