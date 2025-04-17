using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropIndex(
                name: "IX_OrderInProducts_ProductId",
                table: "OrderInProducts");
        }
    }
}
