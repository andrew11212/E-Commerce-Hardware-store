namespace FutureTechnologyE_Commerce.Models.ViewModels
{
	public class OrderConfirmationViewModel
	{
		public int OrderId { get; set; }
		public DateTime OrderDate { get; set; }
		public decimal OrderTotal { get; set; }
		public DateTime EstimatedDeliveryDate { get; set; }

		public ShippingAddressDto ShippingAddress { get; set; } = new();
		public List<OrderItemDto> OrderItems { get; set; } = new();
	}
}
