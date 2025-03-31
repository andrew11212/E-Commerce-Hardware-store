﻿using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FutureTechnologyE_Commerce.Models.ViewModels
{
	public class LaptopViewModel
	{
		public Laptop Laptop { get; set; }
		[ValidateNever]
		public IEnumerable<SelectListItem> CategoryList { get; set; } = default!;
		[ValidateNever]
		public IEnumerable<SelectListItem> ProductTypeList { get; set; } = default!;
		[ValidateNever]
		public IEnumerable<SelectListItem> BrandList { get; set; } = default!;
	}
}
