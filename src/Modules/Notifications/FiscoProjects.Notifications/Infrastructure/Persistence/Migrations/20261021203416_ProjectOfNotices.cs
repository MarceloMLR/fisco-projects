using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProjectOfNotices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                schema: "notifications",
                table: "digest_items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_ProjectId",
                schema: "notifications",
                table: "notifications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_digest_items_ProjectId",
                schema: "notifications",
                table: "digest_items",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_ProjectId",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_digest_items_ProjectId",
                schema: "notifications",
                table: "digest_items");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                schema: "notifications",
                table: "digest_items");
        }
    }
}
