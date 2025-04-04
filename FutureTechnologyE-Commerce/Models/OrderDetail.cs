using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutureTechnologyE_Commerce.Models
{
	public class OrderDetail
	{
		public int Id { get; set; }

		[Required]
		public int OrderId { get; set; }
		[ForeignKey(nameof(OrderId))]
		public OrderHeader orderHeader { get; set; } = default!;

		[Required]
		public int ProductId { get; set; }
		[ForeignKey(nameof(ProductId))]
		public Product Product { get; set; } = default!;

		[Required]
		[Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
		public int Count { get; set; }
		
		[Required]
		[Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
		[DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
		public double Price { get; set; }
	}
}