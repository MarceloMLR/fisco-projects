using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FiscoProjects.Documents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Documents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "documents");

            migrationBuilder.CreateTable(
                name: "default_folders",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsAcceptance = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_default_folders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "folders",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsAcceptance = table.Column<bool>(type: "bit", nullable: false),
                    StorageFolderId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_folders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FolderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    StatusChangedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SchedulePhaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduleItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_documents_folders_FolderId",
                        column: x => x.FolderId,
                        principalSchema: "documents",
                        principalTable: "folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_versions",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    StorageVersionId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_versions_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "documents",
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "documents",
                table: "default_folders",
                columns: new[] { "Id", "IsAcceptance", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("019a0a10-0000-7000-8000-000000000001"), false, true, "01 Gestão do projeto" },
                    { new Guid("019a0a10-0000-7000-8000-000000000002"), false, true, "02 Contratos e compras" },
                    { new Guid("019a0a10-0000-7000-8000-000000000003"), false, true, "03 Requisitos e especificações" },
                    { new Guid("019a0a10-0000-7000-8000-000000000004"), false, true, "04 Atas de reunião" },
                    { new Guid("019a0a10-0000-7000-8000-000000000005"), false, true, "05 Entregas e manuais" },
                    { new Guid("019a0a10-0000-7000-8000-000000000006"), true, true, "06 Termos de aceite" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_default_folders_Name",
                schema: "documents",
                table: "default_folders",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_DocumentId_Number",
                schema: "documents",
                table: "document_versions",
                columns: new[] { "DocumentId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_FolderId_FileId",
                schema: "documents",
                table: "documents",
                columns: new[] { "FolderId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_ProjectId_Name",
                schema: "documents",
                table: "documents",
                columns: new[] { "ProjectId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_folders_ProjectId_Name",
                schema: "documents",
                table: "folders",
                columns: new[] { "ProjectId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "default_folders",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "document_versions",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "folders",
                schema: "documents");
        }
    }
}
