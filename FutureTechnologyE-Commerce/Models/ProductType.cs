using System.ComponentModel.DataAnnotations;

namespace FutureTechnologyE_Commerce.Models
{
	public class ProductType
	{
		[Key]
		public int ProductTypeID { get; set; }

		[Required]
		[StringLength(50)]
		public string Name { get; set; }

		public virtual ICollection<Product> Products { get; set; }
	}
}
