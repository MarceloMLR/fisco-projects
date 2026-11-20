using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenWare.Tickets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TicketImpacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkedItems",
                schema: "tickets",
                table: "ticket_events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlinkedItems",
                schema: "tickets",
                table: "ticket_events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ticket_impacts",
                schema: "tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_impacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_impacts_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "tickets",
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_impacts_ProjectId",
                schema: "tickets",
                table: "ticket_impacts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_impacts_TicketId_ScheduleItemId",
                schema: "tickets",
                table: "ticket_impacts",
                columns: new[] { "TicketId", "ScheduleItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_impacts",
                schema: "tickets");

            migrationBuilder.DropColumn(
                name: "LinkedItems",
                schema: "tickets",
                table: "ticket_events");

            migrationBuilder.DropColumn(
                name: "UnlinkedItems",
                schema: "tickets",
                table: "ticket_events");
        }
    }
}
