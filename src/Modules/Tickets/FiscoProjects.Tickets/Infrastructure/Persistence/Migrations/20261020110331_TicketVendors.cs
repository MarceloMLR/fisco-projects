using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Tickets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TicketVendors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FromVendorStatus",
                schema: "tickets",
                table: "ticket_events",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "tickets",
                table: "ticket_events",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToVendorStatus",
                schema: "tickets",
                table: "ticket_events",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VendorTicketId",
                schema: "tickets",
                table: "ticket_events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vendors",
                schema: "tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PortalUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vendor_tickets",
                schema: "tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    OpenedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RegisteredById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendor_tickets_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "tickets",
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vendor_tickets_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "tickets",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_tickets_TicketId_RegisteredAt",
                schema: "tickets",
                table: "vendor_tickets",
                columns: new[] { "TicketId", "RegisteredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_tickets_VendorId",
                schema: "tickets",
                table: "vendor_tickets",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_Name",
                schema: "tickets",
                table: "vendors",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vendor_tickets",
                schema: "tickets");

            migrationBuilder.DropTable(
                name: "vendors",
                schema: "tickets");

            migrationBuilder.DropColumn(
                name: "FromVendorStatus",
                schema: "tickets",
                table: "ticket_events");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "tickets",
                table: "ticket_events");

            migrationBuilder.DropColumn(
                name: "ToVendorStatus",
                schema: "tickets",
                table: "ticket_events");

            migrationBuilder.DropColumn(
                name: "VendorTicketId",
                schema: "tickets",
                table: "ticket_events");
        }
    }
}
