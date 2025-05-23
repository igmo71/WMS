using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DataVersion",
                table: "Warehouse",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DataVersion",
                table: "Products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DataVersion",
                table: "OrdersInArchive",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DataVersion",
                table: "OrdersIn",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataVersion",
                table: "Warehouse");

            migrationBuilder.DropColumn(
                name: "DataVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DataVersion",
                table: "OrdersInArchive");

            migrationBuilder.DropColumn(
                name: "DataVersion",
                table: "OrdersIn");
        }
    }
}
