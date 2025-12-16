using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCenter.Web.Migrations
{
    /// <inheritdoc />
    public partial class TrainerAreaAndMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Uzmanlik",
                table: "Egitmenler");

            migrationBuilder.AddColumn<bool>(
                name: "Aktif",
                table: "Egitmenler",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Egitmenler",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Egitmenler",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KullaniciAdi",
                table: "Egitmenler",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Maas",
                table: "Egitmenler",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalonId",
                table: "Egitmenler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SifreHash",
                table: "Egitmenler",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "Egitmenler",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Mesajlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GonderenId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AliciId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    GonderimTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Okundu = table.Column<bool>(type: "bit", nullable: false),
                    KonusmaTipi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RandevuId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesajlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mesajlar_AspNetUsers_AliciId",
                        column: x => x.AliciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mesajlar_AspNetUsers_GonderenId",
                        column: x => x.GonderenId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Randevular_RandevuId",
                        column: x => x.RandevuId,
                        principalTable: "Randevular",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UzmanlikAlanlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UzmanlikAlanlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EgitmenUzmanliklari",
                columns: table => new
                {
                    EgitmenId = table.Column<int>(type: "int", nullable: false),
                    UzmanlikAlaniId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EgitmenUzmanliklari", x => new { x.EgitmenId, x.UzmanlikAlaniId });
                    table.ForeignKey(
                        name: "FK_EgitmenUzmanliklari_Egitmenler_EgitmenId",
                        column: x => x.EgitmenId,
                        principalTable: "Egitmenler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EgitmenUzmanliklari_UzmanlikAlanlari_UzmanlikAlaniId",
                        column: x => x.UzmanlikAlaniId,
                        principalTable: "UzmanlikAlanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Egitmenler_ApplicationUserId",
                table: "Egitmenler",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Egitmenler_SalonId",
                table: "Egitmenler",
                column: "SalonId");

            migrationBuilder.CreateIndex(
                name: "IX_EgitmenUzmanliklari_UzmanlikAlaniId",
                table: "EgitmenUzmanliklari",
                column: "UzmanlikAlaniId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_AliciId",
                table: "Mesajlar",
                column: "AliciId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_GonderenId",
                table: "Mesajlar",
                column: "GonderenId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_RandevuId",
                table: "Mesajlar",
                column: "RandevuId");

            migrationBuilder.AddForeignKey(
                name: "FK_Egitmenler_AspNetUsers_ApplicationUserId",
                table: "Egitmenler",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Egitmenler_Salonlar_SalonId",
                table: "Egitmenler",
                column: "SalonId",
                principalTable: "Salonlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Egitmenler_AspNetUsers_ApplicationUserId",
                table: "Egitmenler");

            migrationBuilder.DropForeignKey(
                name: "FK_Egitmenler_Salonlar_SalonId",
                table: "Egitmenler");

            migrationBuilder.DropTable(
                name: "EgitmenUzmanliklari");

            migrationBuilder.DropTable(
                name: "Mesajlar");

            migrationBuilder.DropTable(
                name: "UzmanlikAlanlari");

            migrationBuilder.DropIndex(
                name: "IX_Egitmenler_ApplicationUserId",
                table: "Egitmenler");

            migrationBuilder.DropIndex(
                name: "IX_Egitmenler_SalonId",
                table: "Egitmenler");

            migrationBuilder.DropColumn(
                name: "Aktif",
                table: "Egitmenler");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Egitmenler");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Egitmenler");

            migrationBuilder.DropColumn(
                name: "KullaniciAdi",
                table: "Egitmenler");

            migrationBuilder.DropColumn(
                name: "Maas",
                table: "Egitmenler");

            migrationBuilder.DropColumn(
                name: "SalonId",
                table: "Egitmenler");

            migrationBuilder.DropColumn(
                name: "SifreHash",
                table: "Egitmenler");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "Egitmenler");

            migrationBuilder.AddColumn<string>(
                name: "Uzmanlik",
                table: "Egitmenler",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }
    }
}
