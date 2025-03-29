using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

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
		public int? ParentCategoryID { get; set; }

		public virtual Category ParentCategory { get; set; }
		public virtual ICollection<Category> Subcategories { get; set; }
		public virtual ICollection<Product> Products { get; set; }
	}
}
