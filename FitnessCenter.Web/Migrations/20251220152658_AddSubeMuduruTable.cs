using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCenter.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSubeMuduruTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubeMudurler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdSoyad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SalonId = table.Column<int>(type: "int", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubeMudurler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubeMudurler_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SubeMudurler_Salonlar_SalonId",
                        column: x => x.SalonId,
                        principalTable: "Salonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubeMudurler_ApplicationUserId",
                table: "SubeMudurler",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubeMudurler_SalonId",
                table: "SubeMudurler",
                column: "SalonId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubeMudurler");
        }
    }
}
