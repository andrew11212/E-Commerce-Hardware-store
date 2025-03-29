using System.ComponentModel.DataAnnotations;

namespace FutureTechnologyE_Commerce.Models
{
	public class Keyboard:Product
	{
		[StringLength(50)]
		public string Layout { get; set; }=string.Empty;
		public bool Backlit { get; set; }
		public bool Mechanical { get; set; }
	}
}
