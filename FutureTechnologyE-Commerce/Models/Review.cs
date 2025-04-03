using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FutureTechnologyE_Commerce.Models
{
	public class Review
	{
		[Key]
		public int ReviewID { get; set; }

		[ForeignKey("Product")]
		public int ProductID { get; set; }

		[ForeignKey("User")]
		public string UserID { get; set; } =string.Empty;

		[Required]
		[Range(1, 5)]
		public int Rating { get; set; }

		public string Comment { get; set; }

		[Required]
		public DateTime ReviewDate { get; set; }
		[ValidateNever]
		public virtual Product Product { get; set; }
		[ValidateNever]
		public virtual ApplicationUser User { get; set; }
	}
}
