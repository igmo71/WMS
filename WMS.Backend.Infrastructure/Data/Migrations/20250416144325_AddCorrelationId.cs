using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrelationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "OrdersInArchive",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "OrderInProductsArchive",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "OrdersInArchive");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "OrderInProductsArchive");
        }
    }
}
