using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Users.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserAnonymization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnonymizedAt",
                schema: "users",
                table: "users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnonymousNumber",
                schema: "users",
                table: "users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_AnonymousNumber",
                schema: "users",
                table: "users",
                column: "AnonymousNumber",
                unique: true,
                filter: "[AnonymousNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_AnonymousNumber",
                schema: "users",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AnonymizedAt",
                schema: "users",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AnonymousNumber",
                schema: "users",
                table: "users");
        }
    }
}
