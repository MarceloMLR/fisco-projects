using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FiscoProjects.Tickets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TicketDeadlines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstRespondedAt",
                schema: "tickets",
                table: "tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirstResponseDays",
                schema: "tickets",
                table: "tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FirstResponseDueOn",
                schema: "tickets",
                table: "tickets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PausedBusinessDays",
                schema: "tickets",
                table: "tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedSince",
                schema: "tickets",
                table: "tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolutionDays",
                schema: "tickets",
                table: "tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ResolutionDueOn",
                schema: "tickets",
                table: "tickets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromPriority",
                schema: "tickets",
                table: "ticket_events",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToPriority",
                schema: "tickets",
                table: "ticket_events",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ticket_deadlines",
                schema: "tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    FirstResponseDays = table.Column<int>(type: "int", nullable: false),
                    ResolutionDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_deadlines", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "tickets",
                table: "ticket_deadlines",
                columns: new[] { "Id", "FirstResponseDays", "Priority", "ResolutionDays" },
                values: new object[,]
                {
                    { new Guid("0199f5a3-0000-7000-8000-000000000001"), 1, "Low", 10 },
                    { new Guid("0199f5a3-0000-7000-8000-000000000002"), 1, "Medium", 5 },
                    { new Guid("0199f5a3-0000-7000-8000-000000000003"), 1, "High", 2 },
                    { new Guid("0199f5a3-0000-7000-8000-000000000004"), 1, "Critical", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_deadlines_Priority",
                schema: "tickets",
                table: "ticket_deadlines",
                column: "Priority",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_deadlines",
                schema: "tickets");

            migrationBuilder.DropColumn(
                name: "FirstRespondedAt",
                schema: "tickets",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "FirstResponseDays",
                schema: "tickets",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "FirstResponseDueOn",
                schema: "tickets",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "PausedBusinessDays",
                schema: "tickets",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "PausedSince",
                schema: "tickets",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "ResolutionDays",
                schema: "tickets",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "ResolutionDueOn",
                schema: "tickets",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "FromPriority",
                schema: "tickets",
                table: "ticket_events");

            migrationBuilder.DropColumn(
                name: "ToPriority",
                schema: "tickets",
                table: "ticket_events");
        }
    }
}
