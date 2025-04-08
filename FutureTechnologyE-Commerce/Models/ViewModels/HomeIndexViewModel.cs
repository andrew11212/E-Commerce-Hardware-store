namespace FutureTechnologyE_Commerce.Models.ViewModels
{
	public class HomeIndexViewModel
	{
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 9;
		public int TotalCount { get; set; }
		public string SearchString { get; set; } = string.Empty;
		public IEnumerable<Product>? Products { get; set; }
		public IEnumerable<Product>? Accessories { get; set; }
		public IEnumerable<Laptop>? Laptops { get; set; }
		public string Category { get; set; } = string.Empty;
		public List<string>? CategoryOptions { get; set; }
		public IEnumerable<Review>? TopReviews { get; set; }
		public IEnumerable<Promotion>? Promotions { get; set; }
	}
}
