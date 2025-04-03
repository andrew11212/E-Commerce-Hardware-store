using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FutureTechnologyE_Commerce.Models.ViewModels
{
	public class LaptopViewModel
	{
		[ValidateNever]
		public Laptop Laptop { get; set; } = default!;
		[ValidateNever]
		public IEnumerable<SelectListItem> CategoryList { get; set; } = default!;
		[ValidateNever]
		public IEnumerable<SelectListItem> BrandList { get; set; } = default!;
	}
}
