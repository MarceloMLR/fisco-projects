using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FiscoProjects.Tickets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TicketFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                schema: "tickets",
                table: "tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                schema: "tickets",
                table: "tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ticket_events",
                schema: "tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_events_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "tickets",
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ticket_transitions",
                schema: "tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    From = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    To = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Actors = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequiresMessage = table.Column<bool>(type: "bit", nullable: false),
                    Automatic = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_transitions", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "tickets",
                table: "ticket_transitions",
                columns: new[] { "Id", "Action", "Actors", "Automatic", "From", "RequiresMessage", "To" },
                values: new object[,]
                {
                    { new Guid("0199f5a2-0000-7000-8000-000000000001"), "StartTriage", "Attendant", false, "Open", false, "Triage" },
                    { new Guid("0199f5a2-0000-7000-8000-000000000002"), "FirstResponse", "Attendant", true, "Open", true, "InProgress" },
                    { new Guid("0199f5a2-0000-7000-8000-000000000003"), "StartWork", "Attendant", false, "Triage", false, "InProgress" },
                    { new Guid("0199f5a2-0000-7000-8000-000000000004"), "AskForInformation", "Attendant", false, "InProgress", true, "WaitingForRequester" },
                    { new Guid("0199f5a2-0000-7000-8000-000000000005"), "Reply", "Attendant, Requester", true, "WaitingForRequester", true, "InProgress" },
                    { new Guid("0199f5a2-0000-7000-8000-000000000006"), "Resolve", "Attendant", false, "InProgress", false, "Resolved" },
                    { new Guid("0199f5a2-0000-7000-8000-000000000007"), "ConfirmAndClose", "Requester, Manager, Administrator", false, "Resolved", false, "Closed" },
                    { new Guid("0199f5a2-0000-7000-8000-000000000008"), "AutoClose", "System", true, "Resolved", false, "Closed" },
                    { new Guid("0199f5a2-0000-7000-8000-000000000009"), "Reopen", "Requester", false, "Resolved", true, "InProgress" },
                    { new Guid("0199f5a2-0000-7000-8000-000000000010"), "Cancel", "Requester, Manager, Administrator", false, "Open", true, "Closed" },
                    { new Guid("0199f5a2-0000-7000-8000-000000000011"), "Cancel", "Requester, Manager, Administrator", false, "Triage", true, "Closed" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tickets_Status_ResolvedAt",
                schema: "tickets",
                table: "tickets",
                columns: new[] { "Status", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_events_TicketId_At",
                schema: "tickets",
                table: "ticket_events",
                columns: new[] { "TicketId", "At" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_events",
                schema: "tickets");

            migrationBuilder.DropTable(
                name: "ticket_transitions",
                schema: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_Status_ResolvedAt",
                schema: "tickets",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                schema: "tickets",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                schema: "tickets",
                table: "tickets");
        }
    }
}
