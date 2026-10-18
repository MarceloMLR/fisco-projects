using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Users.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SystemRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "users",
                table: "users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.CreateIndex(
                name: "IX_users_Name",
                schema: "users",
                table: "users",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Name",
                schema: "users",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "users",
                table: "users");
        }
    }
}
