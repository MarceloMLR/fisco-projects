using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FiscoProjects.Documents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Links : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "link_categories",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_link_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "links",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_links_link_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "documents",
                        principalTable: "link_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "documents",
                table: "link_categories",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("019a0a11-0000-7000-8000-000000000001"), true, "Sistemas" },
                    { new Guid("019a0a11-0000-7000-8000-000000000002"), true, "Ambientes" },
                    { new Guid("019a0a11-0000-7000-8000-000000000003"), true, "Repositórios" },
                    { new Guid("019a0a11-0000-7000-8000-000000000004"), true, "Painéis" },
                    { new Guid("019a0a11-0000-7000-8000-000000000005"), true, "Outros" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_link_categories_Name",
                schema: "documents",
                table: "link_categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_links_CategoryId",
                schema: "documents",
                table: "links",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_links_ProjectId",
                schema: "documents",
                table: "links",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "links",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "link_categories",
                schema: "documents");
        }
    }
}
