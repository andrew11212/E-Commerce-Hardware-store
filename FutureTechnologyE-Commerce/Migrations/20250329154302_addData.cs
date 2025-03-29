using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FutureTechnologyE_Commerce.Migrations
{
    /// <inheritdoc />
    public partial class addData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Product",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.InsertData(
                table: "Brands",
                columns: new[] { "BrandID", "Name" },
                values: new object[,]
                {
                    { 1, "Apple" },
                    { 2, "Samsung" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryID", "Name", "ParentCategoryID" },
                values: new object[] { 1, "Electronics", null });

            migrationBuilder.InsertData(
                table: "ProductTypes",
                columns: new[] { "ProductTypeID", "Name" },
                values: new object[,]
                {
                    { 1, "Mobile" },
                    { 2, "Laptop" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryID", "Name", "ParentCategoryID" },
                values: new object[,]
                {
                    { 2, "Laptops", 1 },
                    { 3, "Smartphones", 1 }
                });

            migrationBuilder.InsertData(
                table: "Product",
                columns: new[] { "ProductID", "BrandID", "CategoryID", "Description", "ImageUrl", "Name", "Price", "ProductTypeID", "StockQuantity" },
                values: new object[,]
                {
                    { 1, 1, 3, "Latest Apple iPhone", "iphone14.jpg", "iPhone 14", 999.99m, 1, 50 },
                    { 2, 2, 3, "Latest Samsung Smartphone", "galaxys22.jpg", "Galaxy S22", 899.99m, 1, 40 },
                    { 3, 1, 2, "Apple MacBook Pro 16-inch", "macbookpro.jpg", "MacBook Pro", 2499.99m, 2, 20 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Product",
                keyColumn: "ProductID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Product",
                keyColumn: "ProductID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Product",
                keyColumn: "ProductID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "BrandID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "BrandID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "ProductTypeID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "ProductTypeID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryID",
                keyValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Product",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
