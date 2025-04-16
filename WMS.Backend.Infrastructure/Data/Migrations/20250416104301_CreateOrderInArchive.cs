using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateOrderInArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderInProductsArchive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Operation = table.Column<int>(type: "integer", nullable: false),
                    Archive = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderInProductsArchive", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrdersInArchive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Operation = table.Column<int>(type: "integer", nullable: false),
                    Archive = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdersInArchive", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderInProductsArchive");

            migrationBuilder.DropTable(
                name: "OrdersInArchive");
        }
    }
}
