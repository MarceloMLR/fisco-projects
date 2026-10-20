using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Tickets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TicketConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentFolderId",
                schema: "tickets",
                table: "tickets",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionBody",
                schema: "tickets",
                table: "tickets",
                type: "nvarchar(max)",
                maxLength: 200000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ticket_followers",
                schema: "tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Since = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_followers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_followers_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "tickets",
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ticket_messages",
                schema: "tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 200000, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_messages_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "tickets",
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ticket_attachments",
                schema: "tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UploadedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_attachments_ticket_messages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "tickets",
                        principalTable: "ticket_messages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ticket_attachments_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "tickets",
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_attachments_MessageId",
                schema: "tickets",
                table: "ticket_attachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_attachments_TicketId",
                schema: "tickets",
                table: "ticket_attachments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_followers_TicketId_UserId",
                schema: "tickets",
                table: "ticket_followers",
                columns: new[] { "TicketId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ticket_followers_UserId",
                schema: "tickets",
                table: "ticket_followers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_messages_TicketId_SentAt",
                schema: "tickets",
                table: "ticket_messages",
                columns: new[] { "TicketId", "SentAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_attachments",
                schema: "tickets");

            migrationBuilder.DropTable(
                name: "ticket_followers",
                schema: "tickets");

            migrationBuilder.DropTable(
                name: "ticket_messages",
                schema: "tickets");

            migrationBuilder.DropColumn(
                name: "AttachmentFolderId",
                schema: "tickets",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "DescriptionBody",
                schema: "tickets",
                table: "tickets");
        }
    }
}
