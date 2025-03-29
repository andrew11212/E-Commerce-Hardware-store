namespace FutureTechnologyE_Commerce.Models
{
	public class CartViewModel
	{
		public IEnumerable<ShopingCart> CartList { get; set; } = Enumerable.Empty<ShopingCart>();
		public ShopingCart Cart { get; set; } = default!;

		public OrderHeader OrderHeader { get; set; } = default!;
		public OrderDetail OrderDetail { get; set; } = default!;

	}
}
