using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FutureTechnologyE_Commerce.Models
{
	public class ApplicationUser:IdentityUser
	{
		[Required(ErrorMessage = "First name is required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
		[RegularExpression(@"^[a-zA-Z\s\-]+$", ErrorMessage = "First name can only contain letters, spaces, and hyphens")]
		public string first_name { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "Last name is required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
		[RegularExpression(@"^[a-zA-Z\s\-]+$", ErrorMessage = "Last name can only contain letters, spaces, and hyphens")]
		public string last_name { get; set; } = string.Empty;
		
		[StringLength(30, ErrorMessage = "Apartment cannot exceed 30 characters")]
		public string? apartment { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "Street address is required")]
		[StringLength(100, MinimumLength = 3, ErrorMessage = "Street must be between 3 and 100 characters")]
		public string? street { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "Building number/name is required")]
		[StringLength(30, ErrorMessage = "Building cannot exceed 30 characters")]
		public string? building { get; set; } = string.Empty;
		
		// We inherit PhoneNumber from IdentityUser, so this might be redundant
		[Phone(ErrorMessage = "Invalid phone number format")]
		public string? phone_number { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "Country is required")]
		[StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be a 2-letter code")]
		[RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "Country must be a 2-letter code (e.g., EG)")]
		public string? country { get; set; } = "EG"; // Default to Egypt
		
		[StringLength(10, ErrorMessage = "Floor cannot exceed 10 characters")]
		public string? floor { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "State/City is required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "State/City must be between 2 and 50 characters")]
		public string? state { get; set; } = string.Empty;
	}
}
