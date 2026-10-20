using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmailOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.CreateTable(
                name: "outbox_emails",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ToAddress = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    ToName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HtmlBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TextBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ForgottenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_emails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_emails_Kind_CreatedAt",
                schema: "notifications",
                table: "outbox_emails",
                columns: new[] { "Kind", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_emails_Status_NextAttemptAt",
                schema: "notifications",
                table: "outbox_emails",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_emails",
                schema: "notifications");
        }
    }
}
