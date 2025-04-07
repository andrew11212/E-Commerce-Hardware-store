using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutureTechnologyE_Commerce.Migrations
{
    /// <inheritdoc />
    public partial class dropMouseAndKeypoardModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Keyboards");

            migrationBuilder.DropTable(
                name: "Mice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Keyboards",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Backlit = table.Column<bool>(type: "bit", nullable: false),
                    Layout = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Mechanical = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Keyboards", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_Keyboards_Product_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Product",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mice",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    DPI = table.Column<int>(type: "int", nullable: false),
                    NumberOfButtons = table.Column<int>(type: "int", nullable: false),
                    Wireless = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mice", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_Mice_Product_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Product",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
