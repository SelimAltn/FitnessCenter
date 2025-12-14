using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCenter.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAiLogCachingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CevapMetni",
                table: "AiLoglar",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddColumn<int>(
                name: "DurationMs",
                table: "AiLoglar",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "AiLoglar",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InputHash",
                table: "AiLoglar",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCached",
                table: "AiLoglar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccess",
                table: "AiLoglar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "AiLoglar",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseJson",
                table: "AiLoglar",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "AiLoglar");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "AiLoglar");

            migrationBuilder.DropColumn(
                name: "InputHash",
                table: "AiLoglar");

            migrationBuilder.DropColumn(
                name: "IsCached",
                table: "AiLoglar");

            migrationBuilder.DropColumn(
                name: "IsSuccess",
                table: "AiLoglar");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "AiLoglar");

            migrationBuilder.DropColumn(
                name: "ResponseJson",
                table: "AiLoglar");

            migrationBuilder.AlterColumn<string>(
                name: "CevapMetni",
                table: "AiLoglar",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
