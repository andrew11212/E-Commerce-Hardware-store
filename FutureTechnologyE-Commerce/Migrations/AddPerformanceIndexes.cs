using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutureTechnologyE_Commerce.Migrations
{
    /// <summary>
    /// Migration to add database indexes for performance optimization
    /// </summary>
    public partial class AddPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Product table indexes
            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryID",
                table: "Products",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandID",
                table: "Products",
                column: "BrandID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsBestseller",
                table: "Products",
                column: "IsBestseller");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            // Laptop table indexes
            migrationBuilder.CreateIndex(
                name: "IX_Laptops_CategoryID",
                table: "Laptops",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Laptops_BrandID",
                table: "Laptops",
                column: "BrandID");

            // Review table indexes
            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductID",
                table: "Reviews",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserID",
                table: "Reviews",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Rating",
                table: "Reviews",
                column: "Rating");

            // OrderHeader table indexes
            migrationBuilder.CreateIndex(
                name: "IX_OrderHeaders_ApplicationUserId",
                table: "orderHeaders",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHeaders_OrderStatus",
                table: "orderHeaders",
                column: "OrderStatus");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHeaders_OrderDate",
                table: "orderHeaders",
                column: "OrderDate");

            // OrderDetail table indexes
            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_OrderHeaderId",
                table: "orderDetails",
                column: "OrderHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ProductId",
                table: "orderDetails",
                column: "ProductId");

            // ShoppingCart table indexes
            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ApplicationUserId",
                table: "shopingCarts",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ProductId",
                table: "shopingCarts",
                column: "ProductId");

            // Inventory table indexes
            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductId",
                table: "Inventories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_Quantity",
                table: "Inventories",
                column: "Quantity");

            // Promotion table indexes
            migrationBuilder.CreateIndex(
                name: "IX_Promotions_IsActive",
                table: "Promotions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_StartDate_EndDate",
                table: "Promotions",
                columns: new[] { "StartDate", "EndDate" });

            // Notification table indexes
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            // Category table indexes
            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name");

            // Brand table indexes
            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all indexes in reverse order
            migrationBuilder.DropIndex(name: "IX_Brands_Name", table: "Brands");
            migrationBuilder.DropIndex(name: "IX_Categories_Name", table: "Categories");
            migrationBuilder.DropIndex(name: "IX_Notifications_CreatedAt", table: "Notifications");
            migrationBuilder.DropIndex(name: "IX_Notifications_IsRead", table: "Notifications");
            migrationBuilder.DropIndex(name: "IX_Notifications_UserId", table: "Notifications");
            migrationBuilder.DropIndex(name: "IX_Promotions_StartDate_EndDate", table: "Promotions");
            migrationBuilder.DropIndex(name: "IX_Promotions_IsActive", table: "Promotions");
            migrationBuilder.DropIndex(name: "IX_Inventories_Quantity", table: "Inventories");
            migrationBuilder.DropIndex(name: "IX_Inventories_ProductId", table: "Inventories");
            migrationBuilder.DropIndex(name: "IX_ShoppingCarts_ProductId", table: "shopingCarts");
            migrationBuilder.DropIndex(name: "IX_ShoppingCarts_ApplicationUserId", table: "shopingCarts");
            migrationBuilder.DropIndex(name: "IX_OrderDetails_ProductId", table: "orderDetails");
            migrationBuilder.DropIndex(name: "IX_OrderDetails_OrderHeaderId", table: "orderDetails");
            migrationBuilder.DropIndex(name: "IX_OrderHeaders_OrderDate", table: "orderHeaders");
            migrationBuilder.DropIndex(name: "IX_OrderHeaders_OrderStatus", table: "orderHeaders");
            migrationBuilder.DropIndex(name: "IX_OrderHeaders_ApplicationUserId", table: "orderHeaders");
            migrationBuilder.DropIndex(name: "IX_Reviews_Rating", table: "Reviews");
            migrationBuilder.DropIndex(name: "IX_Reviews_UserID", table: "Reviews");
            migrationBuilder.DropIndex(name: "IX_Reviews_ProductID", table: "Reviews");
            migrationBuilder.DropIndex(name: "IX_Laptops_BrandID", table: "Laptops");
            migrationBuilder.DropIndex(name: "IX_Laptops_CategoryID", table: "Laptops");
            migrationBuilder.DropIndex(name: "IX_Products_Name", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Products_IsBestseller", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Products_BrandID", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Products_CategoryID", table: "Products");
        }
    }
}
