using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FutureTechnologyE_Commerce.Models
{
	public class Category

	{
		[Key]
		public int CategoryID { get; set; }

		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[ForeignKey("ParentCategory")]
		[ValidateNever]

		public int? ParentCategoryID { get; set; }
		[ValidateNever]
		public virtual Category ParentCategory { get; set; }
		[ValidateNever]

		public virtual ICollection<Category> Subcategories { get; set; }
		[ValidateNever]
		public virtual ICollection<Product> Products { get; set; }
	}
}
