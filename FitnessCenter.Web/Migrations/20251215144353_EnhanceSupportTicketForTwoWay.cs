using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCenter.Web.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceSupportTicketForTwoWay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminCevap",
                table: "SupportTickets",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminId",
                table: "SupportTickets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AdminMailGonderildi",
                table: "SupportTickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CevapTarihi",
                table: "SupportTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KullaniciAdi",
                table: "SupportTickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "KullaniciMailGonderildi",
                table: "SupportTickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AdminId",
                table: "SupportTickets",
                column: "AdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportTickets_AspNetUsers_AdminId",
                table: "SupportTickets",
                column: "AdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportTickets_AspNetUsers_AdminId",
                table: "SupportTickets");

            migrationBuilder.DropIndex(
                name: "IX_SupportTickets_AdminId",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "AdminCevap",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "AdminId",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "AdminMailGonderildi",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "CevapTarihi",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "KullaniciAdi",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "KullaniciMailGonderildi",
                table: "SupportTickets");
        }
    }
}
