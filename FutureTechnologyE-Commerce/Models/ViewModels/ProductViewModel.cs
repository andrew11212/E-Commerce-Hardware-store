using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FutureTechnologyE_Commerce.Models.ViewModels
{
	public class ProductViewModel
	{
		public Product product { get; set; }
		[ValidateNever]
		public IEnumerable<SelectListItem> CategoryList { get; set; } = default!;
	}
}
