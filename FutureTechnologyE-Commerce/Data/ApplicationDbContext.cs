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
		public DbSet<Mouse> Mice { get; set; }
		public DbSet<Keyboard> Keyboards { get; set; }
		public DbSet<ProductType> ProductTypes { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<Brand> Brands { get; set; }
		//public DbSet<Order> Orders { get; set; }
		//public DbSet<OrderItem> OrderItems { get; set; }
		public DbSet<Review> Reviews { get; set; }
		public DbSet<Product> products { get; set; }

		public DbSet<ApplicationUser> applicationUsers { get; set; }

		public DbSet<ShopingCart> shopingCarts { get; set; }

		public DbSet<OrderHeader> orderHeaders { get; set; }
		public DbSet<OrderDetail> orderDetails { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Use Table-Per-Type (TPT) for product inheritance
			modelBuilder.Entity<Laptop>().ToTable("Laptops");
			modelBuilder.Entity<Mouse>().ToTable("Mice");
			modelBuilder.Entity<Keyboard>().ToTable("Keyboards");

			// Ensure a user can only review a product once
			modelBuilder.Entity<Review>()
				.HasIndex(r => new { r.ProductID, r.UserID })
				.IsUnique();

            modelBuilder.Entity<Category>().HasData(
               new Category { CategoryID = 1, Name = "Electronics" },
               new Category { CategoryID = 2, Name = "Laptops", ParentCategoryID = 1 },
               new Category { CategoryID = 3, Name = "Smartphones", ParentCategoryID = 1 }
           );

            modelBuilder.Entity<Brand>().HasData(
                new Brand { BrandID = 1, Name = "Apple" },
                new Brand { BrandID = 2, Name = "Samsung" }
            );

            modelBuilder.Entity<ProductType>().HasData(
                new ProductType { ProductTypeID = 1, Name = "Mobile" },
                new ProductType { ProductTypeID = 2, Name = "Laptop" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { ProductID = 1, Name = "iPhone 14", Description = "Latest Apple iPhone", Price = 999.99M, ImageUrl = "iphone14.jpg", CategoryID = 3, BrandID = 1, StockQuantity = 50, ProductTypeID = 1 },
                new Product { ProductID = 2, Name = "Galaxy S22", Description = "Latest Samsung Smartphone", Price = 899.99M, ImageUrl = "galaxys22.jpg", CategoryID = 3, BrandID = 2, StockQuantity = 40, ProductTypeID = 1 },
                new Product { ProductID = 3, Name = "MacBook Pro", Description = "Apple MacBook Pro 16-inch", Price = 2499.99M, ImageUrl = "macbookpro.jpg", CategoryID = 2, BrandID = 1, StockQuantity = 20, ProductTypeID = 2 }
            );
        }
	}
}
