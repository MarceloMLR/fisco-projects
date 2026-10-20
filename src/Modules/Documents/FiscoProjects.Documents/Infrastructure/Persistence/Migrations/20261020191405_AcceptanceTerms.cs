using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Documents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AcceptanceTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "acceptance_terms",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MilestoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RequestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnsweredById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefusalReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acceptance_terms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_acceptance_terms_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "documents",
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "acceptance_events",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acceptance_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_acceptance_events_acceptance_terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "documents",
                        principalTable: "acceptance_terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_acceptance_events_TermId_At",
                schema: "documents",
                table: "acceptance_events",
                columns: new[] { "TermId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_acceptance_terms_DocumentId",
                schema: "documents",
                table: "acceptance_terms",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_acceptance_terms_ProjectId_RecipientId",
                schema: "documents",
                table: "acceptance_terms",
                columns: new[] { "ProjectId", "RecipientId" });

            migrationBuilder.CreateIndex(
                name: "IX_acceptance_terms_Status",
                schema: "documents",
                table: "acceptance_terms",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acceptance_events",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "acceptance_terms",
                schema: "documents");
        }
    }
}
