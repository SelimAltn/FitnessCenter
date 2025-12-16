using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCenter.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAiLogPollingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestId",
                table: "AiLoglar",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AiLoglar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "AiLoglar");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AiLoglar");
        }
    }
}
