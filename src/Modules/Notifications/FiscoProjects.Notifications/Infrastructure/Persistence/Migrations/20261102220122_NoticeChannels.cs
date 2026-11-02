using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NoticeChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlsoByEmail",
                schema: "notifications",
                table: "teams_notices");

            migrationBuilder.DropColumn(
                name: "EmailedInstead",
                schema: "notifications",
                table: "teams_notices");

            migrationBuilder.AddColumn<bool>(
                name: "ByEmail",
                schema: "notifications",
                table: "notice_preferences",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ByTeams",
                schema: "notifications",
                table: "notice_preferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // The channels already chosen (ADR-0151): E-mail turns on only the e-mail, Teams only Teams, and Both the two.
            migrationBuilder.Sql(
                "UPDATE [notifications].[notice_preferences] SET " +
                "[ByEmail] = CASE WHEN [Channel] = 'Teams' THEN 0 ELSE 1 END, " +
                "[ByTeams] = CASE WHEN [Channel] = 'Email' THEN 0 ELSE 1 END;");

            migrationBuilder.DropColumn(
                name: "Channel",
                schema: "notifications",
                table: "notice_preferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                schema: "notifications",
                table: "notice_preferences",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Both");

            migrationBuilder.Sql(
                "UPDATE [notifications].[notice_preferences] SET [Channel] = " +
                "CASE WHEN [ByEmail] = 1 AND [ByTeams] = 0 THEN 'Email' WHEN [ByEmail] = 0 AND [ByTeams] = 1 THEN 'Teams' ELSE 'Both' END;");

            migrationBuilder.DropColumn(
                name: "ByEmail",
                schema: "notifications",
                table: "notice_preferences");

            migrationBuilder.DropColumn(
                name: "ByTeams",
                schema: "notifications",
                table: "notice_preferences");

            migrationBuilder.AddColumn<bool>(
                name: "AlsoByEmail",
                schema: "notifications",
                table: "teams_notices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EmailedInstead",
                schema: "notifications",
                table: "teams_notices",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
