using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FutureTechnologyE_Commerce.Models
{
	public class ApplicationUser:IdentityUser
	{
		[Required]
		public string Name { get; set; } = string.Empty;
		public string? StreetAddress { get; set; } = string.Empty;
		public string? City { get; set; } = string.Empty;
		public string? State { get; set; } = string.Empty;
		public string? PostalCode { get; set; } = string.Empty;

	}
}
