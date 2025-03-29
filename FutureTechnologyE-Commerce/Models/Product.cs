using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FutureTechnologyE_Commerce.Models
{
	public class Product
	{
		[Key]
		public int ProductID { get; set; }

		[Required]
		[StringLength(255)]
		public string Name { get; set; } = string.Empty;

		public string? Description { get; set; }

		[Required]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Price { get; set; }
		public string? ImageUrl { get; set; }
		[ForeignKey("Category")]
		public int CategoryID { get; set; }

		[ForeignKey("Brand")]
		public int BrandID { get; set; }

		[Required]
		public int StockQuantity { get; set; }

		[ForeignKey("ProductType")]
		public int ProductTypeID { get; set; }

		public virtual Category Category { get; set; } = default!;
		public virtual Brand Brand { get; set; } = default!;
		public virtual ProductType ProductType { get; set; } =default!;
	}
}

