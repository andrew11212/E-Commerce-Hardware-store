﻿using FutureTechnologyE_Commerce.Models;
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
