using FutureTechnologyE_Commerce.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace FutureTechnologyE_Commerce.Data
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}
		public DbSet<Product> Products { get; set; }
		public DbSet<Laptop> Laptops { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<Brand> Brands { get; set; }
		public DbSet<Review> Reviews { get; set; }
		public DbSet<Promotion> Promotions { get; set; }
		public DbSet<Product> products { get; set; }
		public DbSet<Inventory> Inventories { get; set; }
		public DbSet<InventoryLog> InventoryLogs { get; set; }
		public DbSet<Notification> Notifications { get; set; }

		public DbSet<ApplicationUser> applicationUsers { get; set; }

		public DbSet<ShopingCart> shopingCarts { get; set; }

		public DbSet<OrderHeader> orderHeaders { get; set; }
		public DbSet<OrderDetail> orderDetails { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Use Table-Per-Type (TPT) for product inheritance
			modelBuilder.Entity<Laptop>().ToTable("Laptops");
			

			// Ensure a user can only review a product once
			modelBuilder.Entity<Review>()
				.HasIndex(r => new { r.ProductID, r.UserID })
				.IsUnique();

			// Performance Indexes
			// Product indexes
			modelBuilder.Entity<Product>()
				.HasIndex(p => p.CategoryID)
				.HasDatabaseName("IX_Products_CategoryID");

			modelBuilder.Entity<Product>()
				.HasIndex(p => p.BrandID)
				.HasDatabaseName("IX_Products_BrandID");

			modelBuilder.Entity<Product>()
				.HasIndex(p => p.IsBestseller)
				.HasDatabaseName("IX_Products_IsBestseller");

			modelBuilder.Entity<Product>()
				.HasIndex(p => p.Name)
				.HasDatabaseName("IX_Products_Name");

			// Review indexes
			modelBuilder.Entity<Review>()
				.HasIndex(r => r.ProductID)
				.HasDatabaseName("IX_Reviews_ProductID");

			modelBuilder.Entity<Review>()
				.HasIndex(r => r.UserID)
				.HasDatabaseName("IX_Reviews_UserID");

			modelBuilder.Entity<Review>()
				.HasIndex(r => r.Rating)
				.HasDatabaseName("IX_Reviews_Rating");

			// OrderHeader indexes
			modelBuilder.Entity<OrderHeader>()
				.HasIndex(o => o.ApplicationUserId)
				.HasDatabaseName("IX_OrderHeaders_ApplicationUserId");

			modelBuilder.Entity<OrderHeader>()
				.HasIndex(o => o.OrderStatus)
				.HasDatabaseName("IX_OrderHeaders_OrderStatus");

			modelBuilder.Entity<OrderHeader>()
				.HasIndex(o => o.OrderDate)
				.HasDatabaseName("IX_OrderHeaders_OrderDate");

			// OrderDetail indexes
			modelBuilder.Entity<OrderDetail>()
				.HasIndex(od => od.OrderId)
				.HasDatabaseName("IX_OrderDetails_OrderHeaderId");

			modelBuilder.Entity<OrderDetail>()
				.HasIndex(od => od.ProductId)
				.HasDatabaseName("IX_OrderDetails_ProductId");

			// ShoppingCart indexes
			modelBuilder.Entity<ShopingCart>()
				.HasIndex(sc => sc.ApplicationUserId)
				.HasDatabaseName("IX_ShoppingCarts_ApplicationUserId");

			modelBuilder.Entity<ShopingCart>()
				.HasIndex(sc => sc.ProductId)
				.HasDatabaseName("IX_ShoppingCarts_ProductId");

			// Inventory indexes
			modelBuilder.Entity<Inventory>()
				.HasIndex(i => i.ProductId)
				.HasDatabaseName("IX_Inventories_ProductId");

			modelBuilder.Entity<Inventory>()
				.HasIndex(i => i.CurrentStock)
				.HasDatabaseName("IX_Inventories_Quantity");

			// Promotion indexes
			modelBuilder.Entity<Promotion>()
				.HasIndex(p => p.IsActive)
				.HasDatabaseName("IX_Promotions_IsActive");

			modelBuilder.Entity<Promotion>()
				.HasIndex(p => new { p.StartDate, p.EndDate })
				.HasDatabaseName("IX_Promotions_StartDate_EndDate");

			// Notification indexes
			modelBuilder.Entity<Notification>()
				.HasIndex(n => n.UserId)
				.HasDatabaseName("IX_Notifications_UserId");

			modelBuilder.Entity<Notification>()
				.HasIndex(n => n.IsRead)
				.HasDatabaseName("IX_Notifications_IsRead");

			modelBuilder.Entity<Notification>()
				.HasIndex(n => n.CreatedDate)
				.HasDatabaseName("IX_Notifications_CreatedAt");

			// Category indexes
			modelBuilder.Entity<Category>()
				.HasIndex(c => c.Name)
				.HasDatabaseName("IX_Categories_Name");

			// Brand indexes
			modelBuilder.Entity<Brand>()
				.HasIndex(b => b.Name)
				.HasDatabaseName("IX_Brands_Name");

            modelBuilder.Entity<Category>().HasData(
               new Category { CategoryID = 1, Name = "mouse" },
               new Category { CategoryID = 2, Name = "Laptops"},
               new Category { CategoryID = 3, Name = "mousepad" },
			   new Category { CategoryID = 4, Name = "Printer" },
			   new Category { CategoryID = 5, Name = "Keypoard" }


		   );

            modelBuilder.Entity<Brand>().HasData(
                new Brand { BrandID = 1, Name = "Apple" },
                new Brand { BrandID = 2, Name = "Samsung" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { ProductID = 1, Name = "AsusTuf", Description = "Latest Apple iPhone", Price = 999.99M, ImageUrl = "iphone14.jpg", CategoryID = 3, BrandID = 1, StockQuantity = 50 },
                new Product { ProductID = 2, Name = "Lenovo", Description = "Latest Samsung Smartphone", Price = 899.99M, ImageUrl = "galaxys22.jpg", CategoryID = 3, BrandID = 2, StockQuantity = 40 },
                new Product { ProductID = 3, Name = "Hp", Description = "Apple MacBook Pro 16-inch", Price = 2499.99M, ImageUrl = "macbookpro.jpg", CategoryID = 2, BrandID = 2, StockQuantity = 20 }
            );
        }
	}
}
