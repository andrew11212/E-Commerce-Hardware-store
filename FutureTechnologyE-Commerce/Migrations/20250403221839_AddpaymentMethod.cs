using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutureTechnologyE_Commerce.Migrations
{
    /// <inheritdoc />
    public partial class AddpaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "orderHeaders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "orderHeaders");
        }
    }
}
