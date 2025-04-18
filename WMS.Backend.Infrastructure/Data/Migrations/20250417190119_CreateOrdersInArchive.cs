using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateOrdersInArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdersInArchive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Operation = table.Column<int>(type: "integer", nullable: false),
                    ArchiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    Archive = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdersInArchive", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderInProducts_ProductId",
                table: "OrderInProducts",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderInProducts_Products_ProductId",
                table: "OrderInProducts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderInProducts_Products_ProductId",
                table: "OrderInProducts");

            migrationBuilder.DropTable(
                name: "OrdersInArchive");

            migrationBuilder.DropIndex(
                name: "IX_OrderInProducts_ProductId",
                table: "OrderInProducts");
        }
    }
}
