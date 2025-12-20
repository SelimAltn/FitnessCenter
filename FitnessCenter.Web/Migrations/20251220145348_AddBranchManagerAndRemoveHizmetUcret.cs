using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCenter.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchManagerAndRemoveHizmetUcret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ucret",
                table: "Hizmetler");

            migrationBuilder.DropColumn(
                name: "IsCached",
                table: "AiLoglar");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "AiLoglar");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AiLoglar");

            migrationBuilder.AddColumn<string>(
                name: "ManagerId",
                table: "Salonlar",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerUserId",
                table: "Salonlar",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Salonlar_ManagerId",
                table: "Salonlar",
                column: "ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Salonlar_AspNetUsers_ManagerId",
                table: "Salonlar",
                column: "ManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Salonlar_AspNetUsers_ManagerId",
                table: "Salonlar");

            migrationBuilder.DropIndex(
                name: "IX_Salonlar_ManagerId",
                table: "Salonlar");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Salonlar");

            migrationBuilder.DropColumn(
                name: "ManagerUserId",
                table: "Salonlar");

            migrationBuilder.AddColumn<decimal>(
                name: "Ucret",
                table: "Hizmetler",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsCached",
                table: "AiLoglar",
                type: "bit",
                nullable: false,
                defaultValue: false);

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
    }
}
