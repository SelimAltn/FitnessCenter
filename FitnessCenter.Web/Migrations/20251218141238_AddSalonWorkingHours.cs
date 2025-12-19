using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCenter.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonWorkingHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "AcilisSaati",
                table: "Salonlar",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Is24Hours",
                table: "Salonlar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "KapanisSaati",
                table: "Salonlar",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcilisSaati",
                table: "Salonlar");

            migrationBuilder.DropColumn(
                name: "Is24Hours",
                table: "Salonlar");

            migrationBuilder.DropColumn(
                name: "KapanisSaati",
                table: "Salonlar");
        }
    }
}
