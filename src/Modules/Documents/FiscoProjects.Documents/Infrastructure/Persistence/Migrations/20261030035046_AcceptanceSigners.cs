using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Documents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AcceptanceSigners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "RecipientId",
                schema: "documents",
                table: "acceptance_terms",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ProviderDocumentId",
                schema: "documents",
                table: "acceptance_terms",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureProvider",
                schema: "documents",
                table: "acceptance_terms",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ActorId",
                schema: "documents",
                table: "acceptance_events",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "SignerId",
                schema: "documents",
                table: "acceptance_events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "acceptance_signers",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefusalReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProviderSignerId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acceptance_signers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_acceptance_signers_acceptance_terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "documents",
                        principalTable: "acceptance_terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_acceptance_terms_SignatureProvider_ProviderDocumentId",
                schema: "documents",
                table: "acceptance_terms",
                columns: new[] { "SignatureProvider", "ProviderDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_acceptance_signers_TermId_Position",
                schema: "documents",
                table: "acceptance_signers",
                columns: new[] { "TermId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acceptance_signers",
                schema: "documents");

            migrationBuilder.DropIndex(
                name: "IX_acceptance_terms_SignatureProvider_ProviderDocumentId",
                schema: "documents",
                table: "acceptance_terms");

            migrationBuilder.DropColumn(
                name: "ProviderDocumentId",
                schema: "documents",
                table: "acceptance_terms");

            migrationBuilder.DropColumn(
                name: "SignatureProvider",
                schema: "documents",
                table: "acceptance_terms");

            migrationBuilder.DropColumn(
                name: "SignerId",
                schema: "documents",
                table: "acceptance_events");

            migrationBuilder.AlterColumn<Guid>(
                name: "RecipientId",
                schema: "documents",
                table: "acceptance_terms",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ActorId",
                schema: "documents",
                table: "acceptance_events",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
