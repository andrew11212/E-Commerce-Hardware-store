using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FutureTechnologyE_Commerce.Models
{
	public class Laptop:Product
	{
		[StringLength(100)]
		public string Processor { get; set; }

		[StringLength(50)]
		public string RAM { get; set; }

		[StringLength(50)]
		public string Storage { get; set; }

		[Column(TypeName = "decimal(4,1)")]
		public decimal ScreenSize { get; set; }

		[StringLength(100)]
		public string GraphicsCard { get; set; }
	}
}
