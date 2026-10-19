using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Projects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleSubtasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                schema: "projects",
                table: "schedule_items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_items_ParentId",
                schema: "projects",
                table: "schedule_items",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_schedule_items_schedule_items_ParentId",
                schema: "projects",
                table: "schedule_items",
                column: "ParentId",
                principalSchema: "projects",
                principalTable: "schedule_items",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_schedule_items_schedule_items_ParentId",
                schema: "projects",
                table: "schedule_items");

            migrationBuilder.DropIndex(
                name: "IX_schedule_items_ParentId",
                schema: "projects",
                table: "schedule_items");

            migrationBuilder.DropColumn(
                name: "ParentId",
                schema: "projects",
                table: "schedule_items");
        }
    }
}
