using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FutureTechnologyE_Commerce.Models
{
	public class ApplicationUser:IdentityUser
	{
		[Required]
		public string first_name { get; set; } = string.Empty;
		public string last_name { get; set; } = string.Empty;
		public string? apartment { get; set; } = string.Empty;
		public string? street { get; set; } = string.Empty;
		public string? building { get; set; } = string.Empty;
		public string? phone_number { get; set; } = string.Empty;
		public string? country { get; set; } = string.Empty;
		public string? floor { get; set; } = string.Empty;
		public string? state{ get; set; } = string.Empty;

	}
}
