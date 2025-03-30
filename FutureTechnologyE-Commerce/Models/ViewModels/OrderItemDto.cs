namespace FutureTechnologyE_Commerce.Models.ViewModels
{
	public class OrderItemDto
	{
		public string ProductName { get; set; } = string.Empty;
		public string ImageUrl { get; set; } = string.Empty;
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal Total => Quantity * UnitPrice;
	}
}
