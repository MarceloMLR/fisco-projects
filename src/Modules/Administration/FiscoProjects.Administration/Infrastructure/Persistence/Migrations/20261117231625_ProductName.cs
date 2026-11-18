using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscoProjects.Administration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductName : Migration
    {
        // The name of the product changed (ADR-0159). Only the installation that still shows the old name of the product
        // gets the new one: a name the Administrator chose stays.
        private const string Rename =
            "UPDATE [administration].[installation_settings] SET [Name] = N'{0}' WHERE [Name] = N'{1}';";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(string.Format(Rename, "GerenWare", "FiscoProjects"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(string.Format(Rename, "FiscoProjects", "GerenWare"));
        }
    }
}
