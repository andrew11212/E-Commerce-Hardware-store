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
		}
	}
}
