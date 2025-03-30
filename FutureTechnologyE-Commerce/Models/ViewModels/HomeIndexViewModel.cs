namespace FutureTechnologyE_Commerce.Models.ViewModels
{
	public class HomeIndexViewModel
	{

		public IEnumerable<Product> Products { get; set; } = default!;
			public List<Laptop> Laptops { get; set; } = new List<Laptop>();
	}
}
