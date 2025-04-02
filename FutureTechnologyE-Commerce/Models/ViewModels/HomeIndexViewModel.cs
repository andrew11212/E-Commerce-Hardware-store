namespace FutureTechnologyE_Commerce.Models.ViewModels
{
	public class HomeIndexViewModel
	{
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public int TotalCount { get; set; }
		public string SearchString { get; set; } = string.Empty;
		public string Category { get; set; } = string.Empty;
		public IEnumerable<Product> Products { get; set; } = default!;
		public List<Laptop> Laptops { get; set; } = new List<Laptop>();
	}
}
