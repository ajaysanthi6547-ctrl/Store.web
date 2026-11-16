using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Santhi.Store.Migrations
{
    /// <inheritdoc />
    public partial class AddStockDeductedToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StockDeducted",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockDeducted",
                table: "Orders");
        }
    }
}
