﻿using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace FutureTechnologyE_Commerce.Models
{
	public class Brand
	{
		[Key]
		public int BrandID { get; set; }

		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[ValidateNever] 
		public virtual ICollection<Product> Products { get; set; }
	}
}
