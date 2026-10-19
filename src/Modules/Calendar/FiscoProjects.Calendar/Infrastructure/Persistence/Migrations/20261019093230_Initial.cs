using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FiscoProjects.Calendar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "calendar");

            migrationBuilder.CreateTable(
                name: "calculated_holidays",
                schema: "calendar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsConsidered = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calculated_holidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "local_holidays",
                schema: "calendar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    RepeatsEveryYear = table.Column<bool>(type: "bit", nullable: false),
                    IsConsidered = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_holidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "work_calendar",
                schema: "calendar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkingDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_calendar", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "calendar",
                table: "calculated_holidays",
                columns: new[] { "Id", "IsConsidered", "Key" },
                values: new object[,]
                {
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b11"), true, "NewYearsDay" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b12"), false, "CarnivalMonday" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b13"), false, "CarnivalTuesday" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b14"), false, "AshWednesday" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b15"), true, "GoodFriday" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b16"), true, "Tiradentes" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b17"), true, "LabourDay" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b18"), false, "CorpusChristi" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b19"), true, "IndependenceDay" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1a"), true, "OurLadyOfAparecida" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1b"), false, "PublicServantsDay" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1c"), true, "AllSoulsDay" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1d"), true, "RepublicDay" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1e"), true, "BlackConsciousnessDay" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b1f"), false, "ChristmasEve" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b20"), true, "ChristmasDay" },
                    { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b21"), false, "NewYearsEve" }
                });

            migrationBuilder.InsertData(
                schema: "calendar",
                table: "work_calendar",
                columns: new[] { "Id", "WorkingDays" },
                values: new object[] { new Guid("019a0127-3c40-7b6e-8f21-5d9c4a7e3b10"), 31 });

            migrationBuilder.CreateIndex(
                name: "IX_calculated_holidays_Key",
                schema: "calendar",
                table: "calculated_holidays",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_local_holidays_Date",
                schema: "calendar",
                table: "local_holidays",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calculated_holidays",
                schema: "calendar");

            migrationBuilder.DropTable(
                name: "local_holidays",
                schema: "calendar");

            migrationBuilder.DropTable(
                name: "work_calendar",
                schema: "calendar");
        }
    }
}
