using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Tickets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TicketChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FromPersonId",
                schema: "tickets",
                table: "ticket_events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromTeam",
                schema: "tickets",
                table: "ticket_events",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ToPersonId",
                schema: "tickets",
                table: "ticket_events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToTeam",
                schema: "tickets",
                table: "ticket_events",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromPersonId",
                schema: "tickets",
                table: "ticket_events");

            migrationBuilder.DropColumn(
                name: "FromTeam",
                schema: "tickets",
                table: "ticket_events");

            migrationBuilder.DropColumn(
                name: "ToPersonId",
                schema: "tickets",
                table: "ticket_events");

            migrationBuilder.DropColumn(
                name: "ToTeam",
                schema: "tickets",
                table: "ticket_events");
        }
    }
}
